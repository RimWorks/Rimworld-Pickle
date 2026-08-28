using System;
using LudeonTK;
using Pickle.Input;
using Pickle.UI;
using Verse;

namespace Pickle.Runtime;

public static class PickleDebugActions {
  [DebugAction("Pickle", "pump smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void PumpSmokeDebugAction() {
    PickleDriver.EnsureExists();
    PumpSmoke.Run();
  }

  [DebugAction("Pickle", "fixture smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void FixtureSmokeDebugAction() {
    PickleDriver.EnsureExists();
    FixtureSmoke.Run();
  }

  [DebugAction("Pickle", "run session smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void RunSessionSmokeDebugAction() {
    PickleDriver.EnsureExists();
    RunSessionSmoke.Run();
  }

  [DebugAction("Pickle", "runner window", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void RunnerWindowDebugAction() {
    PickleDriver.EnsureExists();
    Find.WindowStack.Add(RunnerWindow.Instance);
  }

  [DebugAction("Pickle", "event synth smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void EventSynthSmokeDebugAction() {
    PickleDriver.EnsureExists();
    EventSynthSmoke.Run();
  }

  [DebugAction("Pickle", "widget capture smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void WidgetCaptureSmokeDebugAction() {
    PickleDriver.EnsureExists();
    WidgetCaptureSmoke.Run();
  }

  [DebugAction("Pickle", "tag click smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void TagClickSmokeDebugAction() {
    PickleDriver.EnsureExists();
    TagClickSmoke.Run();
  }

  [DebugAction("Pickle", "tag overlay", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void TagOverlayDebugAction() {
    PickleDriver.EnsureExists();
    TagStore.SessionActive = true;
    TagOverlay.Enabled = !TagOverlay.Enabled;
  }

  [DebugAction("Pickle", "run suite", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static async void RunSuiteDebugAction() {
    PickleDriver.EnsureExists();
    try {
      await SuiteRunner.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }
}
