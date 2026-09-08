using System;
using System.Reflection;
using LudeonTK;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Input;

/// <summary>
/// Key events reach the UI by reinvoking UIRootOnGUI. Clicks cannot: GUI.Window
/// dispatches content through a native InternalCall, so the input backend drives real OS input.
/// </summary>
public static class EventSynth {
  // Used to also carry ClickDown/ClickUp, arming a two-pass MouseDown-then-MouseUp
  private static PendingKind? pendingKind;
  private static PendingAction? pendingAction;
  private static KeyCode pendingKeyCode;
  private static bool reentrant;
  private static Exception? lastFailure;

  /// <summary>Which entry point reinvokes to deliver a synthesized key event.</summary>
  // EventQueue used to be a third mechanism, tried for clicks only. Clicks no longer
  // go through any of these (see InputBackends); this enum now exists for RequestKeyEvent.
  public enum Mechanism {
    /// <summary>Reinvokes <c>Find.UIRoot.UIRootOnGUI</c>.</summary>
    UIRootReinvoke,

    /// <summary>Reinvokes <c>Find.WindowStack.WindowStackOnGUI</c>.</summary>
    WindowStackReinvoke,
  }

  private enum PendingKind {
    UIRootReinvoke,
    WindowStackReinvoke,
  }

  // Clicks no longer arm this at all, see InputBackends. Key is single-shot and consumed
  // in one pass, so no hotControl handshake is involved.
  private enum PendingAction {
    Key,
  }

  /// <summary>Stops the debug log window from auto-opening and closes it if it already has.</summary>
  // RimWorld's debug log window auto-opens on any error in dev mode and then eats
  // clicks meant for the dialog under test. canAutoOpen is private, hence reflection.
  public static void SuppressDebugLogAutoOpen() {
    FieldInfo? canAutoOpenField = typeof(EditWindow_Log).GetField(
        "canAutoOpen", BindingFlags.NonPublic | BindingFlags.Static);
    canAutoOpenField?.SetValue(null, false);

    Find.WindowStack.TryRemoveAssignableFromType(typeof(EditWindow_Log), doCloseSound: false);
  }

  /// <summary>Clicks at a screen point through the active <see cref="InputBackends"/> backend.</summary>
  /// <param name="screenPoint">The point to click, in GUI space.</param>
  // Clicks go through real X11 input, not the mechanisms below. Failures still route
  // through lastFailure/TryTakeFailure, so a click never silently does nothing.
  public static void RequestClick(Vector2 screenPoint) {
    lastFailure = null;
    try {
      InputBackends.Current.Click(screenPoint);
    } catch (Exception ex) {
      lastFailure = ex;
    }
  }

  /// <summary>Arms a key event to be delivered on the next matching OnGUI reinvoke.</summary>
  /// <param name="mechanism">Which reinvoke should deliver the event.</param>
  /// <param name="keyCode">The key to send.</param>
  // One KeyDown(Escape) closes a default Dialog_MessageBox in a single pass, with no
  // rect and no hotControl, so it proves injection reaches the UI at all.
  public static void RequestKeyEvent(Mechanism mechanism, KeyCode keyCode) {
    lastFailure = null;
    switch (mechanism) {
      case Mechanism.UIRootReinvoke:
        pendingKeyCode = keyCode;
        Arm(PendingKind.UIRootReinvoke, PendingAction.Key);
        return;
      case Mechanism.WindowStackReinvoke:
        pendingKeyCode = keyCode;
        Arm(PendingKind.WindowStackReinvoke, PendingAction.Key);
        return;
      default:
        throw new ArgumentOutOfRangeException(
            nameof(mechanism), mechanism, "Key events are only supported for the OnGUI-reinvoke mechanisms.");
    }
  }

  /// <summary>Takes and clears the last exception a click or key event raised, if any.</summary>
  /// <param name="failure">The exception that was raised, or <c>null</c> when there was none.</param>
  /// <returns><c>true</c> when a failure was pending.</returns>
  public static bool TryTakeFailure(out Exception? failure) {
    failure = lastFailure;
    lastFailure = null;
    return failure != null;
  }

  /// <summary>Delivers an armed key event if one is pending, called from a patch just before <c>UIRootOnGUI</c>.</summary>
  // Both PendingKind values need a live native OnGUI on the stack, so both use this
  // one entry point. The reentrant guard lets the recursive call pass through.
  public static void BeforeUIRootOnGUI() {
    if (reentrant || pendingKind == null || pendingAction == null) {
      return;
    }

    PendingKind kind = pendingKind.Value;
    PendingAction action = pendingAction.Value;
    KeyCode keyCode = pendingKeyCode;

    Event original = Event.current;
    reentrant = true;
    try {
      Event injected = action switch {
        PendingAction.Key => BuildKeyEvent(keyCode),
        _ => throw new InvalidOperationException($"pickle: unsupported pending action {action}"),
      };
      Event.current = injected;

      if (kind == PendingKind.UIRootReinvoke) {
        Find.UIRoot.UIRootOnGUI();
      } else {
        Find.WindowStack.WindowStackOnGUI();
      }

      // Diagnostic only. hotControl is backed by native code, so reading it after each
      // pass is the only way to see whether MouseDown grabbed control.
      Log.InfoTo("Pickle",
          "event synth debug kind={Kind} action={Action} hotControl={HotControl}",
          [kind, action, GUIUtility.hotControl]);

      pendingAction = null;
      pendingKind = null;
    } catch (Exception ex) {
      lastFailure = ex;
      pendingKind = null;
      pendingAction = null;
    } finally {
      reentrant = false;
      Event.current = original;
    }
  }

  private static void Arm(PendingKind kind, PendingAction action) {
    pendingKind = kind;
    pendingAction = action;
  }

  private static Event BuildKeyEvent(KeyCode keyCode) {
    return new Event {
      type = EventType.KeyDown,
      keyCode = keyCode,
    };
  }
}
