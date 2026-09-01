using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorks.Pickle.Core.Discovery;
using Verse;

namespace RimWorks.Pickle;

public static class SuiteScanner {
  public static List<DiscoveredSuite> DiscoverSuites() {
    List<DiscoveredSuite> suites = [];

    foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading) {
      SuiteLayout layout = SuiteLayout.FromModRoot(mod.RootDir);
      DiscoveredSuite? suite = SuiteProbe.Probe(mod.Name, layout);

      if (suite != null) {
        suites.Add(suite);
      }
    }

    return suites;
  }

  public static void LogSuites(List<DiscoveredSuite> suites) {
    foreach (DiscoveredSuite suite in suites) {
      string featureList = string.Join(", ", suite.FeatureFiles.Select(Path.GetFileName));
      string featureCount = suite.FeatureFiles.Count == 1 ? "1 feature" : $"{suite.FeatureFiles.Count} features";
      int fixtureCount = suite.FixtureFiles.Count;
      int stepsCount = suite.StepsDlls.Count;

      Log.Message($"pickle: suite {suite.ModName}: {featureCount} [{featureList}] {fixtureCount} fixture(s) {stepsCount} steps dll(s)");
    }
  }
}
