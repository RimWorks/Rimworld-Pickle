using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorks.Pickle.Core.Model;

namespace RimWorks.Pickle.Core.Discovery;

/// <summary>Reads the @quickstart: tag and reports a scenario that also loads a fixture.</summary>
public static class QuickstartTag {
  public const string Prefix = "@quickstart:";

  private static readonly TimeSpan MatchTimeout = TimeSpan.FromSeconds(1);

  private static readonly Regex FixtureStep = new Regex(
      "^\\s*the save \"[^\"]+\" is loaded\\s*$",
      RegexOptions.IgnoreCase,
      MatchTimeout);

  /// <summary>Reads the quickstart a tag set asks for, or null when none of them name one.</summary>
  public static string? NameIn(TagSet tags) {
    string? tag = tags.FirstOrDefault(t => t.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase));
    if (tag == null) {
      return null;
    }

    string name = tag.Substring(Prefix.Length).Trim();
    return name.Length == 0 ? null : name;
  }

  /// <summary>One line per scenario whose quickstart tag cannot be honoured, empty when fine.</summary>
  public static IReadOnlyList<string> Problems(FeaturePlan plan) {
    List<string> problems = [];

    foreach (ScenarioPlan scenario in plan.Scenarios) {
      string? unnamed = scenario.Tags.FirstOrDefault(IsUnnamed);
      if (unnamed != null) {
        problems.Add($"scenario '{scenario.Name}' (line {scenario.Line}) has '{unnamed}' with no quickstart name");
        continue;
      }

      string? quickstart = NameIn(scenario.Tags);
      if (quickstart == null) {
        continue;
      }

      // Both build a starting world, so a silent winner would fail every later step
      // against state nobody asked for, reading as a defect in the mod under test.
      StepPlan? fixtureStep = scenario.Steps.FirstOrDefault(s => FixtureStep.IsMatch(s.Text));
      if (fixtureStep != null) {
        problems.Add(
            $"scenario '{scenario.Name}' (line {scenario.Line}) asks for quickstart '{quickstart}' and also "
            + $"runs '{fixtureStep.Text.Trim()}' on line {fixtureStep.Line}; a scenario gets one or the other");
      }
    }

    return problems;
  }

  private static bool IsUnnamed(string tag) {
    return tag.StartsWith(Prefix, StringComparison.OrdinalIgnoreCase)
        && tag.Substring(Prefix.Length).Trim().Length == 0;
  }
}
