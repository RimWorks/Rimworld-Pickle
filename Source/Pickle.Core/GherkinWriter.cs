using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using RimWorks.Pickle.Core.Model;

namespace RimWorks.Pickle.Core;

/// <summary>Writes one planned scenario back out as a feature file holding only that scenario.</summary>
public static class GherkinWriter {
  private const string DefaultKeyword = "Given";

  private const string QuoteDelimiter = "\"\"\"";

  private const string BacktickDelimiter = "```";

  /// <summary>
  /// Builds a feature file with one scenario in it. Background and outline steps are already folded
  /// into the plan, so the text runs on its own without the rest of its source file.
  /// </summary>
  /// <param name="feature">The feature the scenario came from, used for its name.</param>
  /// <param name="scenario">The scenario to write. Its tags carry the feature's tags too.</param>
  /// <returns>Gherkin source that parses back to one scenario with the same steps.</returns>
  public static string Write(FeaturePlan feature, ScenarioPlan scenario) {
    if (feature == null) {
      throw new ArgumentNullException(nameof(feature));
    }

    if (scenario == null) {
      throw new ArgumentNullException(nameof(scenario));
    }

    StringBuilder text = new StringBuilder();
    text.Append("Feature: ").Append(feature.Name).Append('\n');

    if (scenario.Tags.Count > 0) {
      text.Append("  ").Append(string.Join(" ", scenario.Tags.OrderBy(tag => tag, StringComparer.Ordinal))).Append('\n');
    }

    text.Append("  Scenario: ").Append(scenario.Name).Append('\n');

    foreach (StepPlan step in scenario.Steps) {
      text.Append("    ").Append(Keyword(step.Keyword)).Append(' ').Append(step.Text).Append('\n');
      WriteTable(text, step.Table);
      WriteDocString(text, step.DocString);
    }

    return text.ToString();
  }

  private static string Keyword(string keyword) {
    string trimmed = (keyword ?? string.Empty).Trim();
    return trimmed.Length == 0 ? DefaultKeyword : trimmed;
  }

  private static void WriteTable(StringBuilder text, IReadOnlyList<IReadOnlyList<string>> table) {
    foreach (IReadOnlyList<string> row in table) {
      text.Append("      |");
      foreach (string cell in row) {
        text.Append(' ').Append(EscapeCell(cell)).Append(" |");
      }

      text.Append('\n');
    }
  }

  private static string EscapeCell(string cell) {
    return (cell ?? string.Empty)
        .Replace("\\", "\\\\")
        .Replace("|", "\\|")
        .Replace("\n", "\\n");
  }

  private static void WriteDocString(StringBuilder text, string? docString) {
    if (docString == null) {
      return;
    }

    string delimiter = docString.Contains(QuoteDelimiter) ? BacktickDelimiter : QuoteDelimiter;
    text.Append("      ").Append(delimiter).Append('\n');

    foreach (string line in docString.Split('\n')) {
      text.Append("      ").Append(line.TrimEnd('\r')).Append('\n');
    }

    text.Append("      ").Append(delimiter).Append('\n');
  }
}
