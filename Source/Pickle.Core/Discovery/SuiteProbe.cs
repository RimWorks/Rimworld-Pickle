using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RimWorks.Pickle.Core.Discovery;

public static class SuiteProbe {
  public static DiscoveredSuite? Probe(string modName, SuiteLayout layout) {
    if (!Directory.Exists(layout.PickleDir)) {
      return null;
    }

    List<string> featureFiles = FindFiles(layout.FeaturesDir, "*.feature", SearchOption.AllDirectories);
    List<string> fixtureFiles = FindFiles(layout.FixturesDir, "*.rws", SearchOption.TopDirectoryOnly);
    List<string> stepsDlls = FindFiles(layout.AssembliesDir, "*.dll", SearchOption.TopDirectoryOnly);

    return new DiscoveredSuite(modName, layout.FixturesDir, featureFiles, fixtureFiles, stepsDlls);
  }

  private static List<string> FindFiles(string directory, string pattern, SearchOption option) {
    if (!Directory.Exists(directory)) {
      return [];
    }

    return [.. Directory.GetFiles(directory, pattern, option).OrderBy(f => f)];
  }
}
