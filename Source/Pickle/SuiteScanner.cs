using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Fixtures;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle;

/// <summary>Finds which loaded mods ship a Pickle suite and logs what each one found.</summary>
public static class SuiteScanner {
  /// <summary>Probes every running mod's root for a <c>Pickle</c> folder and resolves the suite
  /// layout for the ones that have one.</summary>
  /// <returns>One entry per mod that ships a suite.</returns>
  public static List<DiscoveredSuite> DiscoverSuites() {
    List<DiscoveredSuite> suites = [];

    foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading) {
      // Resolve only for a mod that ships a suite: the probe writes to test writability, and
      // every other mod would get an empty Pickle/Fixtures/ it never asked for.
      if (!Directory.Exists(Path.Combine(mod.RootDir, "Pickle"))) {
        continue;
      }

      SuiteLayout layout = SuiteLayout.FromModRoot(mod.RootDir, FixtureDirectoryResolver.Resolve(mod.RootDir));
      DiscoveredSuite? suite = SuiteProbe.Probe(mod.Name, layout);

      if (suite != null) {
        suites.Add(suite);
      }
    }

    return suites;
  }

  /// <summary>Logs a summary line per suite, plus a warning for every fixture a stale recording shadows.</summary>
  /// <param name="suites">The suites returned by <see cref="DiscoverSuites"/>.</param>
  public static void LogSuites(List<DiscoveredSuite> suites) {
    foreach (DiscoveredSuite suite in suites) {
      string featureList = string.Join(", ", suite.FeatureFiles.Select(Path.GetFileName));
      string featureCount = suite.FeatureFiles.Count == 1 ? "1 feature" : $"{suite.FeatureFiles.Count} features";
      int fixtureCount = suite.FixtureFiles.Count;
      int stepsCount = suite.StepsDlls.Count;

      Log.InfoTo("Pickle",
          "suite {ModName}: {FeatureCount} [{FeatureList}] {FixtureCount} fixture(s) {StepsCount} steps dll(s)",
          [suite.ModName, featureCount, featureList, fixtureCount, stepsCount]);

      // A stale recording that quietly beats the committed copy passes locally and fails in CI.
      foreach (string shadow in suite.ShadowedFixtures) {
        Log.WarnTo("Pickle", "{ModName} fixture {Shadow}", [suite.ModName, shadow]);
      }
    }
  }
}
