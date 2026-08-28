using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using Pickle.Core.Run;

namespace Pickle.Core.Reports;

public static class SummaryMarkdownWriter {
  public static string Write(IReadOnlyList<ScenarioResult> results) {
    int passed = results.Count(r => r.Outcome == ScenarioOutcome.Passed);
    int failed = results.Count(r => r.Outcome == ScenarioOutcome.Failed);
    int skipped = results.Count(r => r.Outcome == ScenarioOutcome.Skipped);

    StringBuilder builder = new StringBuilder();
    builder.Append("# Pickle Run Summary\n\n");
    builder.Append(results.Count).Append(" scenarios: ")
        .Append(passed).Append(" passed, ")
        .Append(failed).Append(" failed, ")
        .Append(skipped).Append(" skipped\n\n");
    builder.Append("| Scenario | Outcome | Duration (ms) |\n");
    builder.Append("|---|---|---|\n");

    foreach (ScenarioResult scenario in results) {
      builder.Append("| ").Append(EscapeCell(scenario.ScenarioName))
          .Append(" | ").Append(scenario.Outcome)
          .Append(" | ").Append(scenario.DurationMs.ToString(CultureInfo.InvariantCulture))
          .Append(" |\n");
    }

    return builder.ToString();
  }

  private static string EscapeCell(string value) {
    return value.Replace("|", "\\|");
  }
}
