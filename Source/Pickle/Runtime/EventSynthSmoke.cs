using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Input;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

public static class EventSynthSmoke {
  public static void Run() {
    PickleDriver.EnsureExists();
    LongEventHandler.QueueLongEvent(() => _ = RunSmoke(), "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
  }

  private static async Task RunSmoke() {
    try {
      PickleDriver driver = PickleDriver.Instance;

      // EditWindow_Log auto-opens on any error in dev mode and eats clicks meant for the
      // dialog. Suppress it before spawning anything.
      EventSynth.SuppressDebugLogAutoOpen();

      Log.Info("pickle: event synth debug UIScale={UIScale}", [Prefs.UIScale]);
      Log.Info("pickle: event synth debug xdotoolAvailable={XdotoolAvailable}", [XdoInput.Available]);

      // One KeyDown(Escape) closes a default Dialog_MessageBox in a single pass, so this
      // proves the UIRootOnGUI reinvoke works without involving a rect or hotControl.
      Dialog_MessageBox keyTestDialog = new Dialog_MessageBox("pickle synth test (key)");
      Find.WindowStack.Add(keyTestDialog);
      await driver.WaitFrames(2);

      EventSynth.RequestKeyEvent(EventSynth.Mechanism.UIRootReinvoke, KeyCode.Escape);
      await driver.WaitFrames(2);

      bool keyClosed = !Find.WindowStack.IsOpen<Dialog_MessageBox>();
      Log.Info("pickle: event synth key event: {Result}", [keyClosed ? "dialog closed" : "dialog still open"]);

      if (!keyClosed) {
        keyTestDialog.Close(false);
        await driver.WaitFrames(1);
      }

      // Click sub-check: real X11 input via XdoInput (xdotool/XTEST), aimed at the
      // button rect captured live off Widgets.ButtonText.
      Dialog_MessageBox dialog = new Dialog_MessageBox("pickle synth test");
      Find.WindowStack.Add(dialog);
      await driver.WaitFrames(2);

      LogWindowStack(dialog);
      Vector2 target = ButtonCenter(dialog);
      EventSynth.RequestClick(target);
      await driver.WaitFrames(4);

      if (EventSynth.TryTakeFailure(out Exception? failure)) {
        LogWindowStack(dialog);
        Log.Error("pickle: event synth smoke failed: click threw: {Failure}", [failure]);
        return;
      }

      bool closed = !Find.WindowStack.IsOpen<Dialog_MessageBox>();
      Log.Info("pickle: event synth click: {Result}", [closed ? "dialog closed" : "dialog still open"]);

      if (!closed) {
        LogWindowStack(dialog);
        Log.Error(
            "pickle: event synth smoke failed: click did not close the dialog. "
            + "target={Target} pointerNow=[{PointerNow}]",
            [XdoInput.ToScreen(target), XdoInput.GetMouseLocation()]);
        return;
      }

      Log.Info("pickle: event synth smoke passed");
    } catch (Exception ex) {
      Log.Error(ex, "pickle: event synth smoke failed with exception");
    }
  }

  // WindowStack.Windows runs bottom to top, per GetWindowAt. Logged around the click
  // so the log shows what could have intercepted it.
  private static void LogWindowStack(Dialog_MessageBox dialog) {
    IList<Window> windows = Find.WindowStack.Windows;
    string stack = string.Join(", ", windows.Select(w => w.GetType().Name));
    bool dialogTopmost = windows.Count > 0 && windows[windows.Count - 1] == dialog;

    Log.Info(
        "pickle: event synth debug windowstack=[{Stack}] (index 0 = bottom, last = topmost)",
        [stack]);
    Log.Info("pickle: event synth debug dialog topmost={Topmost}", [dialogTopmost]);

    Vector2 target = ButtonCenter(dialog);
    foreach (Window window in windows) {
      bool covers = window.windowRect.Contains(target);
      Log.Info(
          "pickle: event synth debug window {Window} layer={Layer} rect={Rect} coversTarget={CoversTarget}",
          [window.GetType().Name, window.layer, window.windowRect, covers]);
    }

    Window? windowAtTarget = Find.WindowStack.GetWindowAt(target);
    Log.Info(
        "pickle: event synth debug target={Target} windowAtTarget={WindowAtTarget} isDialog={IsDialog}",
        [target, windowAtTarget?.GetType().Name ?? "none", windowAtTarget == dialog]);
  }

  // Get button center from TagStore, which was populated by WidgetCapture.ButtonTextPostfix
  // when the button was drawn. Falls back to hand-derived rect math if the tag is not found.
  private static Vector2 ButtonCenter(Dialog_MessageBox dialog) {
    Rect windowRect = dialog.windowRect;
    Vector2 computed = new Vector2(windowRect.x + 476f, windowRect.y + 424.5f);

    bool found = TagStore.TryGet("btn:OK", out Rect capturedRect, out bool duplicate);
    if (found && !duplicate) {
      Log.Info(
          "pickle: event synth debug windowRect={WindowRect} capturedRect={CapturedRect}",
          [windowRect, capturedRect]);
      return capturedRect.center;
    }

    Log.Info(
        "pickle: event synth debug windowRect={WindowRect} computedCenter={ComputedCenter} "
        + "capturedFound={CapturedFound} capturedDuplicate={CapturedDuplicate}",
        [windowRect, computed, found, duplicate]);
    return computed;
  }
}
