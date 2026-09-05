using System.Collections.Generic;
using System.Globalization;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class SummaryMarkdownWriterTests {
  [Fact]
  public void Write_ContainsHeadlineCountLine() {
    List<ScenarioResult> results = ReportWriterTestData.BuildTwoFeatureRun();
    string markdown = SummaryMarkdownWriter.Write(results);

    Assert.Contains("4 scenarios: 2 passed, 1 failed, 1 skipped", markdown);
  }

  [Fact]
  public void Write_ContainsOneRowPerScenario() {
    List<ScenarioResult> results = ReportWriterTestData.BuildTwoFeatureRun();
    string markdown = SummaryMarkdownWriter.Write(results);
    string[] lines = markdown.Split('\n');

    foreach (ScenarioResult scenario in results) {
      string expectedRow = $"| {scenario.ScenarioName} | {scenario.Outcome} | {scenario.DurationMs.ToString(CultureInfo.InvariantCulture)} | {scenario.Attempts} |  |";
      Assert.Contains(expectedRow, lines);
    }
  }

  [Fact]
  public void Write_EmptyResultList_StillHasHeadlineAndHeader() {
    string markdown = SummaryMarkdownWriter.Write(new List<ScenarioResult>());

    Assert.Contains("0 scenarios: 0 passed, 0 failed, 0 skipped", markdown);
    Assert.Contains("| Scenario | Outcome | Duration (ms) | Attempts | Mean tick (ms) |", markdown);
  }

  [Fact]
  public void Write_FlakyRun_CountsFlakesAndMarksTheRow() {
    string markdown = SummaryMarkdownWriter.Write(ReportWriterTestData.BuildFlakyRun());

    Assert.Contains("2 scenarios: 1 passed, 1 failed, 0 skipped, 1 flaky", markdown);
    Assert.Contains("| flaky checkout | Passed | 180 | 3 (flaky) |  |", markdown);

    // Failed on every attempt is a failure, not a flake, however many tries it took.
    Assert.Contains("| stubbornly broken | Failed | 40 | 2 |  |", markdown);
  }

  [Fact]
  public void Write_NoFlakes_LeavesTheCountLineAlone() {
    string markdown = SummaryMarkdownWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());

    Assert.DoesNotContain("flaky", markdown);
  }

  [Fact]
  public void Write_TickCost_ShowsTheMeanAndLeavesUnmeasuredBlank() {
    string markdown = SummaryMarkdownWriter.Write(ReportWriterTestData.BuildTickCostRun());

    Assert.Contains("| waits out a thousand ticks | Passed | 900 | 1 | 3.25 |", markdown);
    Assert.Contains("| reads a def at the main menu | Passed | 4 | 1 |  |", markdown);
  }
}
