using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class SummaryJsonWriterTests {
  [Fact]
  public void Write_CountsMatchSyntheticRun() {
    List<ScenarioResult> results = ReportWriterTestData.BuildTwoFeatureRun();
    string json = SummaryJsonWriter.Write(results, "completed");

    JsonElement root = JsonDocument.Parse(json).RootElement;

    Assert.Equal(4, root.GetProperty("total").GetInt32());
    Assert.Equal(2, root.GetProperty("passed").GetInt32());
    Assert.Equal(1, root.GetProperty("failed").GetInt32());
    Assert.Equal(1, root.GetProperty("skipped").GetInt32());
    Assert.Equal("completed", root.GetProperty("exitReason").GetString());
  }

  [Fact]
  public void Write_ScenariosArray_HasNameOutcomeAndDurationPerScenario() {
    List<ScenarioResult> results = ReportWriterTestData.BuildTwoFeatureRun();
    string json = SummaryJsonWriter.Write(results, "completed");

    JsonElement scenarios = JsonDocument.Parse(json).RootElement.GetProperty("scenarios");
    Assert.Equal(results.Count, scenarios.GetArrayLength());

    for (int i = 0; i < results.Count; i++) {
      JsonElement entry = scenarios[i];
      Assert.Equal(results[i].ScenarioName, entry.GetProperty("name").GetString());
      Assert.Equal(results[i].Outcome.ToString(), entry.GetProperty("outcome").GetString());
      Assert.Equal(results[i].DurationMs, entry.GetProperty("durationMs").GetDouble());
    }
  }

  [Fact]
  public void Write_ExitReasonWithQuotesAndBackslash_RoundTrips() {
    string exitReason = "aborted: \"timeout\" after \\5 retries";
    string json = SummaryJsonWriter.Write([], exitReason);

    JsonElement root = JsonDocument.Parse(json).RootElement;
    Assert.Equal(exitReason, root.GetProperty("exitReason").GetString());
    Assert.Equal(0, root.GetProperty("total").GetInt32());
    Assert.Equal(0, root.GetProperty("scenarios").GetArrayLength());
  }

  [Fact]
  public void Write_FlakyRun_CountsFlakesAndCarriesAttempts() {
    string json = SummaryJsonWriter.Write(ReportWriterTestData.BuildFlakyRun(), "passed");

    JsonElement root = JsonDocument.Parse(json).RootElement;
    Assert.Equal(1, root.GetProperty("flaky").GetInt32());
    Assert.Equal(3, root.GetProperty("scenarios")[0].GetProperty("attempts").GetInt32());
    Assert.Equal(2, root.GetProperty("scenarios")[1].GetProperty("attempts").GetInt32());
  }

  [Fact]
  public void Write_RunWithNoRetries_ReportsZeroFlakyAndOneAttemptEach() {
    string json = SummaryJsonWriter.Write(ReportWriterTestData.BuildTwoFeatureRun(), "passed");

    JsonElement root = JsonDocument.Parse(json).RootElement;
    Assert.Equal(0, root.GetProperty("flaky").GetInt32());
    foreach (JsonElement scenario in root.GetProperty("scenarios").EnumerateArray()) {
      Assert.Equal(1, scenario.GetProperty("attempts").GetInt32());
    }
  }

  [Fact]
  public void Write_TickCost_IsPresentWhenMeasuredAndAbsentWhenNot() {
    string json = SummaryJsonWriter.Write(ReportWriterTestData.BuildTickCostRun(), "passed");

    JsonElement scenarios = JsonDocument.Parse(json).RootElement.GetProperty("scenarios");

    JsonElement cost = scenarios[0].GetProperty("tickCost");
    Assert.Equal(1000, cost.GetProperty("ticks").GetInt32());
    Assert.Equal(3.25, cost.GetProperty("meanMs").GetDouble());
    Assert.Equal(41.5, cost.GetProperty("maxMs").GetDouble());

    // Absent, not zero: a scenario that measured nothing is not the fastest one.
    Assert.False(scenarios[1].TryGetProperty("tickCost", out _));
  }
}
