using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Fixtures;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.UI;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Proves the fixture manager sees a real committed save and reads its header without
/// loading it. The screenshot it leaves is the only way to look at the window from here.
/// </summary>
public static class FixtureManagerSmoke {
  public static async Task Run() {
    try {
      await RunAsync(PickleDriver.Instance);
      Log.Info("pickle: fixture manager smoke passed");
    } catch (Exception ex) {
      Log.Error(ex, "pickle: fixture manager smoke failed");
    }
  }

  private static async Task Capture(string name) {
    string path = Path.Combine(ScreenshotCapture.ReportsDirectory(), name);
    await ScreenshotCapture.CaptureToFile(path);

    if (!File.Exists(path)) {
      throw new InvalidOperationException($"no screenshot at {path}");
    }

    Log.Info("pickle: screenshot at {Path}", [path]);
  }

  private static async Task RunAsync(PickleDriver driver) {
    List<FixtureEntry> entries = [];
    foreach (DiscoveredSuite suite in SuiteScanner.DiscoverSuites()) {
      entries.AddRange(FixtureCatalog.Read(suite.FixturesDir, suite.WritableFixturesDir));
    }

    FixtureEntry? colony = entries.FirstOrDefault(e => e.Name == "test-colony");
    if (colony == null) {
      throw new InvalidOperationException(
          $"the catalog missed test-colony; it found: {string.Join(", ", entries.Select(e => e.Name))}");
    }

    // A real 3MB save, not the synthetic string the unit tests read.
    FixtureHeader header = FixtureHeader.Read(colony.FullPath);
    if (string.IsNullOrEmpty(header.GameVersion) || string.IsNullOrEmpty(header.ScenarioName)) {
      throw new InvalidOperationException(
          $"header read nothing out of {colony.FullPath}: version '{header.GameVersion}', scenario '{header.ScenarioName}'");
    }

    Log.Info(
        "pickle: fixture manager sees test-colony, {Bytes} bytes, {Version}, {Scenario}, {Mods} mods",
        [colony.SizeBytes, header.GameVersion!, header.ScenarioName!, header.ModCount]);

    // Bare menu first: the log viewer and anything else on the stack would cover the
    // Pickle Runner button, which is the thing this shot exists to show.
    foreach (Window open in Find.WindowStack.Windows.ToList()) {
      Find.WindowStack.TryRemove(open, doCloseSound: false);
    }

    await driver.WaitFrames(5);
    await Capture("main-menu.png");

    Find.WindowStack.Add(RunnerWindow.Instance);
    Find.WindowStack.Add(new FixtureManagerDialog());
    await driver.WaitFrames(5);
    await Capture("fixture-manager.png");
  }
}
