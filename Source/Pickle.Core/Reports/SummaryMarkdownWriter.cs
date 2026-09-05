using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Core.Reports;

public static class SummaryMarkdownWriter {
  public static string Write(IReadOnlyList<ScenarioResult> results) {
    int passed = results.Count(r => r.Outcome == ScenarioOutcome.Passed);
    int failed = results.Count(r => r.Outcome == ScenarioOutcome.Failed);
    int skipped = results.Count(r => r.Outcome == ScenarioOutcome.Skipped);
    int flaky = results.Count(RunOutcomes.IsFlaky);

    StringBuilder builder = new StringBuilder();
    builder.Append("# Pickle Run Summary\n\n");
    builder.Append(results.Count).Append(" scenarios: ")
        .Append(passed).Append(" passed, ")
        .Append(failed).Append(" failed, ")
        .Append(skipped).Append(" skipped");

    // Flaky only when there is one. A trailing ", 0 flaky" on every green run trains
    // people to stop reading the line.
    if (flaky > 0) {
      builder.Append(", ").Append(flaky).Append(" flaky");
    }

    builder.Append("\n\n");
    builder.Append("| Scenario | Outcome | Duration (ms) | Attempts | Mean tick (ms) |\n");
    builder.Append("|---|---|---|---|---|\n");

    foreach (ScenarioResult scenario in results) {
      builder.Append("| ").Append(EscapeCell(scenario.ScenarioName))
          .Append(" | ").Append(scenario.Outcome)
          .Append(" | ").Append(scenario.DurationMs.ToString(CultureInfo.InvariantCulture))
          .Append(" | ").Append(RunOutcomes.IsFlaky(scenario) ? $"{scenario.Attempts} (flaky)" : scenario.Attempts.ToString(CultureInfo.InvariantCulture))
          .Append(" | ").Append(scenario.TickCost.HasValue
              ? scenario.TickCost.Value.MeanMs.ToString("0.###", CultureInfo.InvariantCulture)
              : string.Empty)
          .Append(" |\n");
    }

    return builder.ToString();
  }

  private static string EscapeCell(string value) {
    return value.Replace("|", "\\|");
  }
}
