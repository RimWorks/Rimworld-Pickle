using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

public static class RunnerWindowSmoke {
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
        Log.Error("pickle: runner window smoke failed: no features parsed");
        return;
      }

      int failedCount = window.FailedResultsCount;
      if (failedCount > 0) {
        Log.Error("pickle: runner window smoke failed: {FailedCount} scenarios failed", [failedCount]);
        return;
      }

      Log.Info("pickle: runner window smoke passed");
    } catch (Exception ex) {
      Log.Error(ex, "pickle: runner window smoke failed with exception");
    }
  }
}
