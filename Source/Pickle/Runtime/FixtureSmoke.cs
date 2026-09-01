using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Fixtures;
using RimWorks.Pickle.Fixtures;
using RimWorks.Pickle.Runtime;
using Verse;

namespace RimWorks.Pickle;

public static class FixtureSmoke {
  public static async Task Run() {
    PickleDriver driver = PickleDriver.Instance;
    try {
      await RunAsync(driver);
      Log.Message("pickle: fixture smoke passed");
    } catch (Exception ex) {
      Log.Error("pickle: fixture smoke failed: " + ex);
    }
  }

  private static async Task RunAsync(PickleDriver driver) {
    GameDataSaveLoader.SaveGame("__pickle_test_scratch");

    string scratchPath = GenFilePaths.FilePathForSavedGame("__pickle_test_scratch");

    string tempDir = Path.Combine(Path.GetTempPath(), "pickle-fixture-smoke-" + Guid.NewGuid());
    try {
      string scratchFixturesDir = Path.Combine(tempDir, "Pickle", "Fixtures");
      Directory.CreateDirectory(scratchFixturesDir);

      string scratchFixturePath = Path.Combine(scratchFixturesDir, "scratch.rws");
      File.Copy(scratchPath, scratchFixturePath, true);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite scratchSuite = SuiteProbe.Probe("Pickle", layout)!;

      FixtureResolution resolution = FixtureResolver.Resolve("scratch", "Pickle", [scratchSuite]);

      if (resolution.Error != null) {
        throw new InvalidOperationException($"Fixture resolution failed: {resolution.Error.Message}");
      }

      if (resolution.Fixture == null) {
        throw new InvalidOperationException("Fixture resolution returned null fixture and no error");
      }

      await FixtureLoader.LoadFixture(resolution.Fixture.FullPath, driver);

      LogWatch.Arm();
      Log.Error("pickle: deliberate watchdog test error");

      if (LogWatch.ErrorCount != 1) {
        throw new InvalidOperationException($"Expected 1 error in LogWatch, but got {LogWatch.ErrorCount}");
      }

      string? foundError = LogWatch.ErrorsSinceArmed.FirstOrDefault(e => e.Contains("pickle: deliberate watchdog test error"));
      if (foundError == null) {
        throw new InvalidOperationException("Deliberate test error not found in LogWatch");
      }

      // A mark taken after the first error must not see it, and must see the next one.
      // This is what "the save round trips" leans on to blame only the trip.
      long mark = LogWatch.Mark;
      if (LogWatch.ErrorsSince(mark).Count != 0) {
        throw new InvalidOperationException("LogWatch.ErrorsSince reported an error logged before the mark");
      }

      Log.Error("pickle: deliberate marked test error");

      IReadOnlyList<string> sinceMark = LogWatch.ErrorsSince(mark);
      if (sinceMark.Count != 1 || !sinceMark[0].Contains("pickle: deliberate marked test error")) {
        throw new InvalidOperationException(
            $"Expected 1 error since the mark, but got {sinceMark.Count}");
      }

      LogWatch.Disarm();
    } finally {
      try {
        if (Directory.Exists(tempDir)) {
          Directory.Delete(tempDir, true);
        }

        if (File.Exists(scratchPath)) {
          File.Delete(scratchPath);
        }
      } catch {
        // best effort cleanup of a scratch file; a leftover does not fail the smoke
      }
    }
  }
}
