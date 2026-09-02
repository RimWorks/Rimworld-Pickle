using System;
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
    List<string> stepsDlls = FindFiles(layout.AssembliesDir, "*.dll", SearchOption.TopDirectoryOnly);

    List<string> fixtureFiles = FindFixtures(layout, out List<string> shadowed);

    return new DiscoveredSuite(
        modName,
        layout.FixturesDir,
        layout.WritableFixturesDir,
        featureFiles,
        fixtureFiles,
        shadowed,
        stepsDlls);
  }

  // Both directories, the writable one winning on a name clash: re-recording a fixture is how
  // you fix one, and the committed copy would otherwise keep shadowing the new file.
  private static List<string> FindFixtures(SuiteLayout layout, out List<string> shadowed) {
    Dictionary<string, string> byName = new(StringComparer.OrdinalIgnoreCase);
    shadowed = [];

    foreach (string path in FindFiles(layout.FixturesDir, "*.rws", SearchOption.TopDirectoryOnly)) {
      byName[Path.GetFileNameWithoutExtension(path)] = path;
    }

    if (layout.WritableFixturesDir != layout.FixturesDir) {
      foreach (string path in FindFiles(layout.WritableFixturesDir, "*.rws", SearchOption.TopDirectoryOnly)) {
        string name = Path.GetFileNameWithoutExtension(path);
        if (byName.TryGetValue(name, out string? committed)) {
          shadowed.Add($"{name}: using {path}, ignoring {committed}");
        }

        byName[name] = path;
      }
    }

    return [.. byName.Values.OrderBy(f => f, StringComparer.Ordinal)];
  }

  private static List<string> FindFiles(string directory, string pattern, SearchOption option) {
    if (!Directory.Exists(directory)) {
      return [];
    }

    return [.. Directory.GetFiles(directory, pattern, option).OrderBy(f => f)];
  }
}
