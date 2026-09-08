using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>Opens the runner window, runs every feature through it, and checks nothing failed.</summary>
public static class RunnerWindowSmoke {
  /// <summary>Queues the smoke to run once the game finishes its loading long event.</summary>
  public static void Run() {
    PickleDriver.EnsureExists();
    LongEventHandler.QueueLongEvent(() => _ = RunSmoke(), "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
  }

  private static async Task RunSmoke() {
    try {
      PickleDriver driver = PickleDriver.Instance;

      RunnerWindow window = new RunnerWindow();
      Find.WindowStack.Add(window);

      await driver.WaitFrames(2);

      await window.RunAllAndWait();

      if (window.ParsedFeaturesCount < 1) {
        Log.ErrorTo("Pickle", "runner window smoke failed: no features parsed");
        return;
      }

      int failedCount = window.FailedResultsCount;
      if (failedCount > 0) {
        Log.ErrorTo("Pickle", "runner window smoke failed: {FailedCount} scenarios failed", [failedCount]);
        return;
      }

      Log.InfoTo("Pickle", "runner window smoke passed");
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, "runner window smoke failed with exception");
    }
  }
}
