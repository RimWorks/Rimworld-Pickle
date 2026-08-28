using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pickle.Core.Discovery;

namespace Pickle.Core.Fixtures;

public static class FixtureResolver {
  public static FixtureResolution Resolve(string fixtureName, string requestingModName, IReadOnlyList<DiscoveredSuite> suites) {
    DiscoveredSuite? requestingSuite = suites.FirstOrDefault(s => string.Equals(s.ModName, requestingModName, StringComparison.OrdinalIgnoreCase));

    if (requestingSuite != null) {
      string? requestingModFixture = FindFixtureInSuite(fixtureName, requestingSuite);
      if (requestingModFixture != null) {
        return new FixtureResolution(new ResolvedFixture(requestingModFixture));
      }
    }

    List<(string ModName, string Path)> otherMatches = [];
    foreach (DiscoveredSuite suite in suites) {
      if (string.Equals(suite.ModName, requestingModName, StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      string? fixture = FindFixtureInSuite(fixtureName, suite);
      if (fixture != null) {
        otherMatches.Add((suite.ModName, fixture));
      }
    }

    if (otherMatches.Count == 1) {
      return new FixtureResolution(new ResolvedFixture(otherMatches[0].Path));
    }

    if (otherMatches.Count > 1) {
      string sources = string.Join(", ", otherMatches.Select(m => $"{m.ModName} ({m.Path})"));
      string message = $"Multiple fixtures named '{fixtureName}' found: {sources}";
      return new FixtureResolution(new FixtureError(fixtureName, FixtureErrorKind.Duplicate, message));
    }

    List<string> allFixtures = GetAllFixtureNames(suites);
    string knownFixturesMsg = allFixtures.Count == 0
        ? "No fixtures found in any suite."
        : $"Known fixtures: {string.Join(", ", allFixtures)}";
    string notFoundMessage = $"Fixture '{fixtureName}' not found. {knownFixturesMsg}";
    return new FixtureResolution(new FixtureError(fixtureName, FixtureErrorKind.NotFound, notFoundMessage));
  }

  private static string? FindFixtureInSuite(string fixtureName, DiscoveredSuite suite) {
    foreach (string fixtureFile in suite.FixtureFiles) {
      string fileName = Path.GetFileNameWithoutExtension(fixtureFile);
      if (string.Equals(fileName, fixtureName, StringComparison.OrdinalIgnoreCase)) {
        return fixtureFile;
      }
    }

    return null;
  }

  private static List<string> GetAllFixtureNames(IReadOnlyList<DiscoveredSuite> suites) {
    HashSet<string> names = [];
    foreach (DiscoveredSuite suite in suites) {
      foreach (string fixtureFile in suite.FixtureFiles) {
        names.Add(Path.GetFileNameWithoutExtension(fixtureFile));
      }
    }

    return [.. names.OrderBy(n => n)];
  }
}
