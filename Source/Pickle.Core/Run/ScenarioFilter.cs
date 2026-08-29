using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Pickle.Core.Model;

namespace Pickle.Core.Run;

/// <summary>
/// Decides which scenarios a run includes. A filter is a comma separated list of terms
/// and a scenario runs when any one of them picks it.
/// </summary>
public static class ScenarioFilter {
  /// <summary>
  /// Terms are <c>@tag</c>, a mod name, a feature path, <c>path::name</c>,
  /// <c>path:line</c>, or <c>::name</c> to match a scenario in any feature.
  /// </summary>
  public static bool Matches(string modName, string? sourcePath, ScenarioPlan scenario, string term) {
    if (term.StartsWith("@", StringComparison.Ordinal)) {
      return scenario.Tags.Contains(term);
    }

    int nameSplit = term.IndexOf("::", StringComparison.Ordinal);
    if (nameSplit >= 0) {
      string path = term.Substring(0, nameSplit);
      string name = term.Substring(nameSplit + 2);
      return (path.Length == 0 || MatchesPath(sourcePath, path))
          && scenario.Name.IndexOf(name, StringComparison.OrdinalIgnoreCase) >= 0;
    }

    // Only treat a trailing :N as a line when it parses, so a windows path keeps working.
    int lineSplit = term.LastIndexOf(':');
    if (lineSplit > 0 && int.TryParse(term.Substring(lineSplit + 1), out int line)) {
      return MatchesPath(sourcePath, term.Substring(0, lineSplit)) && scenario.Line == line;
    }

    return string.Equals(modName, term, StringComparison.OrdinalIgnoreCase)
        || MatchesPath(sourcePath, term);
  }

  public static IReadOnlyList<string> SplitTerms(string? filter) {
    // Not IsNullOrEmpty: net472 has no NotNullWhen on it, so the compiler still
    // wants a null-forgiving operator after the guard.
    if (filter == null || filter.Length == 0) {
      return [];
    }

    return [.. filter.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0)];
  }

  public static bool MatchesPath(string? sourcePath, string term) {
    if (sourcePath == null) {
      return false;
    }

    string source = sourcePath.Replace('\\', '/');
    string wanted = term.Replace('\\', '/');

    return string.Equals(source, wanted, StringComparison.OrdinalIgnoreCase)
        || source.EndsWith("/" + wanted, StringComparison.OrdinalIgnoreCase)
        || string.Equals(Path.GetFileName(sourcePath), term, StringComparison.OrdinalIgnoreCase);
  }
}
