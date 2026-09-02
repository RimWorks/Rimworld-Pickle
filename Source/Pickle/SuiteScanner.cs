using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Fixtures;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle;

public static class SuiteScanner {
  public static List<DiscoveredSuite> DiscoverSuites() {
    List<DiscoveredSuite> suites = [];
    // Probed against the first mod that ships a suite: they all live under the same Mods
    // directory, so one of them answers for all of them.
    string? writableRoot = FixtureDirectoryResolver.Resolve(FirstSuiteRoot());

    foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading) {
      SuiteLayout layout = SuiteLayout.FromModRoot(mod.RootDir, writableRoot);
      DiscoveredSuite? suite = SuiteProbe.Probe(mod.Name, layout);

      if (suite != null) {
        suites.Add(suite);
      }
    }

    return suites;
  }

  private static string? FirstSuiteRoot() {
    foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading) {
      if (Directory.Exists(Path.Combine(mod.RootDir, "Pickle"))) {
        return mod.RootDir;
      }
    }

    return null;
  }

  public static void LogSuites(List<DiscoveredSuite> suites) {
    foreach (DiscoveredSuite suite in suites) {
      string featureList = string.Join(", ", suite.FeatureFiles.Select(Path.GetFileName));
      string featureCount = suite.FeatureFiles.Count == 1 ? "1 feature" : $"{suite.FeatureFiles.Count} features";
      int fixtureCount = suite.FixtureFiles.Count;
      int stepsCount = suite.StepsDlls.Count;

      Log.Info(
          "pickle: suite {ModName}: {FeatureCount} [{FeatureList}] {FixtureCount} fixture(s) {StepsCount} steps dll(s)",
          [suite.ModName, featureCount, featureList, fixtureCount, stepsCount]);

      // A stale recording that quietly beats the committed copy passes locally and fails in CI.
      foreach (string shadow in suite.ShadowedFixtures) {
        Log.Warn("pickle: {ModName} fixture {Shadow}", [suite.ModName, shadow]);
      }
    }
  }
}
