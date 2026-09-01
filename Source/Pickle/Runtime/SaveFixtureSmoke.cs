using System;
using System.IO;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Fixtures;
using RimWorks.Pickle.UI;
using Verse;

namespace RimWorks.Pickle.Runtime;

public static class SaveFixtureSmoke {
  public static async Task Run() {
    try {
      await RunAsync(PickleDriver.Instance);
      Log.Message("pickle: save fixture smoke passed");
    } catch (Exception ex) {
      Log.Error("pickle: save fixture smoke failed: " + ex);
    }
  }

  private static async Task RunAsync(PickleDriver driver) {
    string fixturePath = Path.Combine("/game/Mods/Pickle/Pickle/Fixtures", "test-colony.rws");
    if (!File.Exists(fixturePath)) {
      throw new InvalidOperationException($"dogfood fixture missing at {fixturePath}");
    }

    await FixtureLoader.LoadFixture(fixturePath, driver);

    // The mod dir is mounted read-only in the harness, so this targets a writable
    // stand-in. What it proves is the save-then-copy path, not the mount.
    string targetRoot = Path.Combine("/out", "save-fixture-smoke");
    if (Directory.Exists(targetRoot)) {
      Directory.Delete(targetRoot, true);
    }

    SuiteLayout layout = SuiteLayout.FromModRoot(targetRoot);
    Directory.CreateDirectory(layout.PickleDir);
    DiscoveredSuite suite = SuiteProbe.Probe("SmokeTarget", layout)!;

    DateTime startedUtc = DateTime.UtcNow;
    SaveFixtureDialog.Save(suite, "smoke-fixture");

    string written = Path.Combine(layout.FixturesDir, "smoke-fixture.rws");
    if (!File.Exists(written)) {
      throw new InvalidOperationException($"fixture was not written to {written}");
    }

    FileInfo info = new FileInfo(written);
    if (info.Length == 0) {
      throw new InvalidOperationException("written fixture is empty");
    }

    // Guards against a leftover from an earlier run passing the check.
    if (info.LastWriteTimeUtc < startedUtc.AddSeconds(-5)) {
      throw new InvalidOperationException($"fixture at {written} is stale, not written by this run");
    }

    // It has to be loadable, or it is not a fixture.
    await FixtureLoader.LoadFixture(written, driver);

    Log.Message($"pickle: save fixture wrote {info.Length} bytes to {written} and reloaded it");
  }
}
