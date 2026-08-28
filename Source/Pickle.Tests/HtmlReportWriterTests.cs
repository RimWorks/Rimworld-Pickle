using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using Pickle.Core.Reports;
using Pickle.Core.Run;
using Xunit;

namespace Pickle.Tests;

public class HtmlReportWriterTests {
  private const string Template = "<html><script id=\"pickle-report\">__PICKLE_REPORT_JSON__</script></html>";

  private static JsonElement Payload(
      List<ScenarioResult> results,
      string exitReason = "failed",
      System.Func<string, byte[]?>? readBytes = null) {
    string json = HtmlReportWriter.BuildPayload(results, exitReason, readBytes);
    return JsonDocument.Parse(json.Replace("<\\/", "</")).RootElement;
  }

  [Fact]
  public void Write_replaces_the_placeholder() {
    string html = HtmlReportWriter.Write(ReportWriterTestData.BuildTwoFeatureRun(), "failed", Template);

    Assert.DoesNotContain("__PICKLE_REPORT_JSON__", html);
    Assert.StartsWith("<html><script", html);
  }

  [Fact]
  public void Payload_groups_scenarios_by_feature() {
    JsonElement root = Payload(ReportWriterTestData.BuildTwoFeatureRun());

    List<string> features = [.. root.GetProperty("features")
        .EnumerateArray()
        .Select(f => f.GetProperty("name").GetString()!)];

    Assert.Equal(["Login", "Checkout"], features);
    Assert.Equal(2, root.GetProperty("features")[0].GetProperty("scenarios").GetArrayLength());
  }

  [Fact]
  public void Payload_carries_counts_and_exit_reason() {
    JsonElement root = Payload(ReportWriterTestData.BuildTwoFeatureRun());

    Assert.Equal(2, root.GetProperty("passed").GetInt32());
    Assert.Equal(1, root.GetProperty("failed").GetInt32());
    Assert.Equal("failed", root.GetProperty("exitReason").GetString());
  }

  [Fact]
  public void Scenario_indexes_are_unique_across_features() {
    JsonElement root = Payload(ReportWriterTestData.BuildTwoFeatureRun());

    List<int> indexes = [.. root.GetProperty("features")
        .EnumerateArray()
        .SelectMany(f => f.GetProperty("scenarios").EnumerateArray())
        .Select(s => s.GetProperty("index").GetInt32())];

    Assert.Equal(indexes.Count, indexes.Distinct().Count());
  }

  [Fact]
  public void Failure_message_and_steps_survive() {
    JsonElement failing = Payload(ReportWriterTestData.BuildTwoFeatureRun())
        .GetProperty("features")[0]
        .GetProperty("scenarios")[1];

    Assert.Equal("Failed", failing.GetProperty("outcome").GetString());
    Assert.Equal(ReportWriterTestData.JUnitFailureMessage, failing.GetProperty("failureMessage").GetString());
    Assert.Equal(2, failing.GetProperty("steps").GetArrayLength());
    Assert.Equal("Failed", failing.GetProperty("steps")[1].GetProperty("status").GetString());
  }

  [Fact]
  public void Readable_attachments_become_data_uris() {
    JsonElement failing = Payload(
            ReportWriterTestData.BuildTwoFeatureRun(),
            readBytes: path => path.EndsWith(".png") ? [1, 2, 3] : null)
        .GetProperty("features")[0]
        .GetProperty("scenarios")[1];

    JsonElement attachments = failing.GetProperty("attachments");
    Assert.StartsWith("data:image/png;base64,", attachments[0].GetProperty("content").GetString());

    // Anything that is not readable keeps its path rather than becoming a broken image.
    Assert.Equal("some note", attachments[1].GetProperty("content").GetString());
  }

  [Fact]
  public void Closing_script_tag_in_a_message_cannot_break_out_of_the_payload() {
    List<ScenarioResult> results =
    [
        new ScenarioResult(
                "evil",
                "Injection",
                new Core.Model.TagSet([]),
                ScenarioOutcome.Failed,
                [new StepResult("Then", "boom", StepStatus.Failed, 1, "</script><script>alert(1)</script>")],
                1,
                "</script><script>alert(1)</script>"),
        ];

    string html = HtmlReportWriter.Write(results, "failed", Template);

    // One closing tag only: the template's own. The payload's are escaped.
    Assert.Equal(1, CountOccurrences(html, "</script>"));
  }

  private static int CountOccurrences(string haystack, string needle) {
    int count = 0;
    int at = haystack.IndexOf(needle, System.StringComparison.Ordinal);
    while (at >= 0) {
      count++;
      at = haystack.IndexOf(needle, at + needle.Length, System.StringComparison.Ordinal);
    }

    return count;
  }
}
