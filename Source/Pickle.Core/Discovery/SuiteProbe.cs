using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorks.Pickle.Core.Fixtures;

namespace RimWorks.Pickle.Core.Discovery;

/// <summary>Walks a mod's Pickle folder and builds the <see cref="DiscoveredSuite"/> found there.</summary>
public static class SuiteProbe {
  /// <summary>Discovers a mod's suite, or <c>null</c> when it has no Pickle folder.</summary>
  /// <param name="modName">The mod the suite belongs to.</param>
  /// <param name="layout">The folders to look under.</param>
  /// <returns>The discovered suite, or <c>null</c> when <paramref name="layout"/> has no Pickle directory.</returns>
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
