using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Core.Reports;

/// <summary>Renders a run's scenario results as the <c>summary.json</c> report, read by the dashboard and by scripts polling a run.</summary>
public static class SummaryJsonWriter {
  /// <summary>Builds the JSON summary for a run. Callers polling mid-run should check <paramref name="exitReason"/>
  /// for <c>in-progress</c> before trusting the counts.</summary>
  /// <param name="results">Every scenario the run produced, in report order.</param>
  /// <param name="exitReason">Why the run stopped, or that it has not stopped yet.</param>
  /// <param name="setName">The mod set the run targeted, or <c>null</c> for an unnamed run so old reports keep parsing unchanged.</param>
  /// <returns>The <c>summary.json</c> document as a string.</returns>
  public static string Write(IReadOnlyList<ScenarioResult> results, string exitReason, string? setName = null) {
    int passed = results.Count(r => r.Outcome == ScenarioOutcome.Passed);
    int failed = results.Count(r => r.Outcome == ScenarioOutcome.Failed);
    int skipped = results.Count(r => r.Outcome == ScenarioOutcome.Skipped);
    int flaky = results.Count(RunOutcomes.IsFlaky);

    StringBuilder builder = new StringBuilder();
    builder.Append('{');
    builder.Append("\"total\":").Append(results.Count).Append(',');
    builder.Append("\"passed\":").Append(passed).Append(',');
    builder.Append("\"failed\":").Append(failed).Append(',');
    builder.Append("\"skipped\":").Append(skipped).Append(',');
    builder.Append("\"flaky\":").Append(flaky).Append(',');
    builder.Append("\"exitReason\":").Append(JsonEscape.Quote(exitReason)).Append(',');

    // Absent from an unnamed run, so every report written before mod sets existed still
    // reads byte for byte the same.
    if (setName != null) {
      builder.Append("\"setName\":").Append(JsonEscape.Quote(setName)).Append(',');
    }

    builder.Append("\"scenarios\":[");

    for (int i = 0; i < results.Count; i++) {
      if (i > 0) {
        builder.Append(',');
      }

      ScenarioResult scenario = results[i];
      builder.Append('{');
      builder.Append("\"name\":").Append(JsonEscape.Quote(scenario.ScenarioName)).Append(',');
      builder.Append("\"outcome\":").Append(JsonEscape.Quote(scenario.Outcome.ToString())).Append(',');
      builder.Append("\"durationMs\":").Append(scenario.DurationMs.ToString(CultureInfo.InvariantCulture)).Append(',');
      builder.Append("\"attempts\":").Append(scenario.Attempts);

      // Absent, not zero. A scenario that measured nothing would otherwise read as the
      // fastest one in the run.
      if (scenario.TickCost.HasValue) {
        (int ticks, double meanMs, double maxMs) = scenario.TickCost.Value;
        builder.Append(",\"tickCost\":{\"ticks\":").Append(ticks)
            .Append(",\"meanMs\":").Append(Number(meanMs))
            .Append(",\"maxMs\":").Append(Number(maxMs)).Append('}');
      }

      builder.Append('}');
    }

    builder.Append(']');
    builder.Append('}');
    return builder.ToString();
  }

  private static string Number(double value) {
    return value.ToString("0.###", CultureInfo.InvariantCulture);
  }
}
