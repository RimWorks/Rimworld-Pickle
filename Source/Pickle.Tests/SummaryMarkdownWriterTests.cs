using System.Collections.Generic;
using System.Globalization;
using Pickle.Core.Reports;
using Pickle.Core.Run;
using Xunit;

namespace Pickle.Tests;

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
      string expectedRow = $"| {scenario.ScenarioName} | {scenario.Outcome} | {scenario.DurationMs.ToString(CultureInfo.InvariantCulture)} |";
      Assert.Contains(expectedRow, lines);
    }
  }

  [Fact]
  public void Write_EmptyResultList_StillHasHeadlineAndHeader() {
    string markdown = SummaryMarkdownWriter.Write(new List<ScenarioResult>());

    Assert.Contains("0 scenarios: 0 passed, 0 failed, 0 skipped", markdown);
    Assert.Contains("| Scenario | Outcome | Duration (ms) |", markdown);
  }
}
