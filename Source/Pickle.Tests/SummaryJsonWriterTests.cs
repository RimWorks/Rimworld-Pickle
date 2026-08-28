using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Pickle.Core.Reports;
using Pickle.Core.Run;
using Xunit;

namespace Pickle.Tests;

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
}
