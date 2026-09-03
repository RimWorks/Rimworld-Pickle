using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorks.Pickle.Core.Fixtures;

namespace RimWorks.Pickle.Core.Discovery;

public static class SuiteProbe {
  public static DiscoveredSuite? Probe(string modName, SuiteLayout layout) {
    if (!Directory.Exists(layout.PickleDir)) {
      return null;
    }

    List<string> featureFiles = FindFiles(layout.FeaturesDir, "*.feature", SearchOption.AllDirectories);
    List<string> stepsDlls = FindFiles(layout.AssembliesDir, "*.dll", SearchOption.TopDirectoryOnly);

    List<FixtureEntry> fixtures = FixtureCatalog.Read(layout.FixturesDir, layout.WritableFixturesDir);
    List<string> fixtureFiles = [.. fixtures.Where(f => !f.IsShadowed).Select(f => f.FullPath)];
    List<string> shadowed = [.. fixtures
        .Where(f => f.ShadowedPath != null)
        .Select(f => $"{f.Name}: using {f.FullPath}, ignoring {f.ShadowedPath}")];

    return new DiscoveredSuite(
        modName,
        layout.FixturesDir,
        layout.WritableFixturesDir,
        featureFiles,
        fixtureFiles,
        shadowed,
        stepsDlls);
  }

  private static List<string> FindFiles(string directory, string pattern, SearchOption option) {
    if (!Directory.Exists(directory)) {
      return [];
    }

    return [.. Directory.GetFiles(directory, pattern, option).OrderBy(f => f)];
  }
}
