using System;
using System.Threading.Tasks;
using LudeonTK;
using RimWorks.Pickle.Input;
using RimWorks.Pickle.UI;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

public static class PickleDebugActions {
  [DebugAction("Pickle", "pump smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void PumpSmokeDebugAction() {
    PickleDriver.EnsureExists();
    _ = PumpSmoke.Run();
  }

  [DebugAction("Pickle", "fixture smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void FixtureSmokeDebugAction() {
    PickleDriver.EnsureExists();
    _ = FixtureSmoke.Run();
  }

  [DebugAction("Pickle", "run session smoke", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void RunSessionSmokeDebugAction() {
    PickleDriver.EnsureExists();
    _ = RunSessionSmoke.Run();
  }

  // no state bits means no requirement. IsAllowedInCurrentGameState ANDs every bit set,
  // so Entry | PlayingOnMap would demand both at once and never show.
  [DebugAction("Pickle", "runner window", allowedGameStates = AllowedGameStates.Invalid)]
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
    _ = TagClickSmoke.Run();
  }

  [DebugAction("Pickle", "tag overlay", allowedGameStates = AllowedGameStates.PlayingOnMap)]
  private static void TagOverlayDebugAction() {
    PickleDriver.EnsureExists();
    TagStore.SessionActive = true;
    TagOverlay.Enabled = !TagOverlay.Enabled;
  }

  // void, not async Task: Delegate.CreateDelegate(typeof(Action)) cannot bind a Task and
  // one that fails aborts the menu build for every mod.
  [DebugAction("Pickle", "run suite", allowedGameStates = AllowedGameStates.Invalid)]
  private static void RunSuiteDebugAction() {
    PickleDriver.EnsureExists();
    _ = RunSuite();
  }

  private static async Task RunSuite() {
    try {
      await SuiteRunner.Run();
    } catch (Exception ex) {
      Log.Error(ex, "pickle: run suite debug action failed");
    }
  }
}
