using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Core.Reports;

public static class SummaryJsonWriter {
  public static string Write(IReadOnlyList<ScenarioResult> results, string exitReason) {
    int passed = results.Count(r => r.Outcome == ScenarioOutcome.Passed);
    int failed = results.Count(r => r.Outcome == ScenarioOutcome.Failed);
    int skipped = results.Count(r => r.Outcome == ScenarioOutcome.Skipped);

    StringBuilder builder = new StringBuilder();
    builder.Append('{');
    builder.Append("\"total\":").Append(results.Count).Append(',');
    builder.Append("\"passed\":").Append(passed).Append(',');
    builder.Append("\"failed\":").Append(failed).Append(',');
    builder.Append("\"skipped\":").Append(skipped).Append(',');
    builder.Append("\"exitReason\":").Append(JsonEscape.Quote(exitReason)).Append(',');
    builder.Append("\"scenarios\":[");

    for (int i = 0; i < results.Count; i++) {
      if (i > 0) {
        builder.Append(',');
      }

      ScenarioResult scenario = results[i];
      builder.Append('{');
      builder.Append("\"name\":").Append(JsonEscape.Quote(scenario.ScenarioName)).Append(',');
      builder.Append("\"outcome\":").Append(JsonEscape.Quote(scenario.Outcome.ToString())).Append(',');
      builder.Append("\"durationMs\":").Append(scenario.DurationMs.ToString(CultureInfo.InvariantCulture));
      builder.Append('}');
    }

    builder.Append(']');
    builder.Append('}');
    return builder.ToString();
  }
}
