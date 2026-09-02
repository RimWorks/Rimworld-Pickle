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

    return new DiscoveredSuite(
        modName,
        layout.FixturesDir,
        layout.WritableFixturesDir,
        featureFiles,
        FindFixtures(layout),
        stepsDlls);
  }

  // Both directories, the writable one winning on a name clash: re-recording a fixture is how
  // you fix one, and the committed copy would otherwise keep shadowing the new file.
  private static List<string> FindFixtures(SuiteLayout layout) {
    Dictionary<string, string> byName = new(StringComparer.OrdinalIgnoreCase);

    foreach (string path in FindFiles(layout.FixturesDir, "*.rws", SearchOption.TopDirectoryOnly)) {
      byName[Path.GetFileNameWithoutExtension(path)] = path;
    }

    if (layout.WritableFixturesDir != layout.FixturesDir) {
      foreach (string path in FindFiles(layout.WritableFixturesDir, "*.rws", SearchOption.TopDirectoryOnly)) {
        byName[Path.GetFileNameWithoutExtension(path)] = path;
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
