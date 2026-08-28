using System;
using System.Threading.Tasks;
using Pickle.Runtime;
using Pickle.UI;
using Verse;

namespace Pickle.Runtime;

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
        Log.Error($"pickle: runner window smoke failed: {failedCount} scenarios failed");
        return;
      }

      Log.Message("pickle: runner window smoke passed");
    } catch (Exception ex) {
      Log.Error($"pickle: runner window smoke failed with exception: {ex}");
    }
  }
}
