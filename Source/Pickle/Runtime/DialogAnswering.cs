using System.Collections.Generic;
using System.Linq;
using RimWorks.Pickle.Core.Ui;
using RimWorks.Pickle.Input;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Clicks a button on a window a scenario asked to have answered, rather than letting suppression
/// drop it. A mod that records only an explicit reply asks again every load until it gets one.
/// </summary>
public static class DialogAnswering {
  private static readonly DialogAnswers Rules = new();
  private static readonly HashSet<Window> Clicked = [];

  private static bool hooked;

  /// <summary>Records the button to click whenever a window of this type opens during the scenario.</summary>
  /// <param name="windowTypeName">The window's simple type name, as <c>GetType().Name</c> reports it.</param>
  /// <param name="buttonLabel">The label on the button to click.</param>
  public static void Answer(string windowTypeName, string buttonLabel) {
    Rules.Add(windowTypeName, buttonLabel);
    if (!hooked) {
      PickleDriver.Instance.AddFrameHook(OnFrame);
      hooked = true;
    }
  }

  /// <summary>Forgets every answer, so one scenario's rules never reach the next.</summary>
  public static void Reset() {
    Rules.Clear();
    Clicked.Clear();
    if (hooked) {
      PickleDriver.Instance.RemoveFrameHook(OnFrame);
      hooked = false;
    }
  }

  /// <summary>Whether a scenario asked for this window to be answered, so suppression must let it through.</summary>
  /// <param name="window">The window about to be added.</param>
  /// <returns><c>true</c> when an answer is registered for the window's type.</returns>
  public static bool Wants(Window? window) {
    return window != null && Rules.TryGet(window.GetType().Name, out _);
  }

  private static void OnFrame() {
    if (!Rules.Any || Find.WindowStack == null) {
      return;
    }

    foreach (Window window in Find.WindowStack.Windows.ToList()) {
      if (Clicked.Contains(window) || !Rules.TryGet(window.GetType().Name, out string label)) {
        continue;
      }

      if (!TagInteractor.TryResolve($"btn:{label}", out Rect rect, out string? _)) {
        continue;
      }

      InteractionRequest.ArmClick(rect);
      Clicked.Add(window);
      Log.InfoTo(
          PickleLog.Channel,
          "answered {Window} with {Button}",
          [window.GetType().Name, label]);
    }
  }
}
