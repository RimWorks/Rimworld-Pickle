using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;

namespace RimWorks.Pickle.Core.Run;

/// <summary>
/// Decides which scenarios a run includes. A filter is a comma separated list of terms
/// and a scenario runs when any one of them picks it.
/// </summary>
public static class ScenarioFilter {
  /// <summary>
  /// Terms are <c>@tag</c>, a mod name, a feature path, <c>path::name</c>,
  /// <c>path:line</c>, or <c>::name</c> to match a scenario in any feature.
  /// </summary>
  /// <param name="modName">The mod the scenario's feature belongs to.</param>
  /// <param name="sourcePath">The feature file's path, or <c>null</c> when it has none.</param>
  /// <param name="scenario">The scenario being tested against the term.</param>
  /// <param name="term">One filter term, as described above.</param>
  /// <returns><c>true</c> when the term picks this scenario.</returns>
  public static bool Matches(string modName, string? sourcePath, ScenarioPlan scenario, string term) {
    if (term.Length > 0 && term[0] == '@') {
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

  /// <summary>Splits a comma separated filter into its trimmed, non-empty terms.</summary>
  /// <param name="filter">The raw filter string, or <c>null</c>.</param>
  /// <returns>The terms, or an empty list when the filter is <c>null</c> or empty.</returns>
  public static IReadOnlyList<string> SplitTerms(string? filter) {
    // Not IsNullOrEmpty: net472 has no NotNullWhen on it, so the compiler still
    // wants a null-forgiving operator after the guard.
    if (filter == null || filter.Length == 0) {
      return [];
    }

    return [.. filter.Split(',').Select(t => t.Trim()).Where(t => t.Length > 0)];
  }

  /// <summary>
  /// Whether a feature path matches a term, comparing case insensitively and treating
  /// <c>\</c> and <c>/</c> as the same separator. A term also matches on the bare file name.
  /// </summary>
  /// <param name="sourcePath">The feature file's path, or <c>null</c>.</param>
  /// <param name="term">The path or file name to match against.</param>
  /// <returns><c>true</c> when the term identifies this path. Always <c>false</c> when <paramref name="sourcePath"/> is <c>null</c>.</returns>
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

  /// <summary>
  /// Narrows parsed features to the ones a filter picks. A filter that matches nothing
  /// throws rather than returning an empty run, which CI otherwise reads as a pass.
  /// </summary>
  /// <param name="parsedFeatures">Every discovered feature and its parsed plan.</param>
  /// <param name="filter">A comma separated filter, or <c>null</c> to keep everything.</param>
  /// <returns>The features that still have at least one matching scenario.</returns>
  public static List<(DiscoveredSuite Suite, FeaturePlan Plan)> FilterFeatures(
      IReadOnlyList<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures, string? filter) {
    IReadOnlyList<string> terms = SplitTerms(filter);
    if (terms.Count == 0) {
      return [.. parsedFeatures];
    }

    List<(DiscoveredSuite Suite, FeaturePlan Plan)> kept = new();
    foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
      List<ScenarioPlan> scenarios = [.. plan.Scenarios
          .Where(s => terms.Any(t => Matches(suite.ModName, plan.SourcePath, s, t)))];
      if (scenarios.Count > 0) {
        kept.Add((suite, new FeaturePlan(plan.Name, plan.Tags, scenarios, plan.SourcePath)));
      }
    }

    if (kept.Count == 0) {
      throw new InvalidOperationException(DescribeNoMatch(parsedFeatures, terms));
    }

    return kept;
  }

  private static string DescribeNoMatch(
      IReadOnlyList<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures, IReadOnlyList<string> terms) {
    StringBuilder message = new StringBuilder()
        .Append("pickle: filter '")
        .Append(string.Join(",", terms))
        .AppendLine("' matched no scenarios.");

    foreach (IGrouping<string, (DiscoveredSuite Suite, FeaturePlan Plan)> suite in
        parsedFeatures.GroupBy(f => f.Suite.ModName, StringComparer.OrdinalIgnoreCase)) {
      message.Append("  ")
          .Append(suite.Count())
          .Append(" features in ")
          .Append(suite.Key)
          .Append(": ")
          .AppendLine(string.Join(", ", suite.Select(f => Path.GetFileName(f.Plan.SourcePath ?? string.Empty))));
    }

    return message.Append("  terms are @tag, mod name, feature path, path::name, path:line, or ::name").ToString();
  }
}
