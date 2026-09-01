using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class MessagesNdjsonWriterTests {
  [Fact]
  public void Write_EveryLineParsesAsIndependentJson() {
    string ndjson = MessagesNdjsonWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    string[] lines = SplitLines(ndjson);

    Assert.NotEmpty(lines);
    foreach (string line in lines) {
      using JsonDocument document = JsonDocument.Parse(line);
      Assert.Single(document.RootElement.EnumerateObject());
    }
  }

  [Fact]
  public void Write_FirstLineIsMeta_LastLineIsTestRunFinished() {
    string ndjson = MessagesNdjsonWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    string[] lines = SplitLines(ndjson);

    Assert.Equal("meta", EnvelopeName(lines[0]));
    Assert.Equal("testRunFinished", EnvelopeName(lines[lines.Length - 1]));
    Assert.Equal(1, lines.Count(l => EnvelopeName(l) == "testRunFinished"));
  }

  [Fact]
  public void Write_EachTestCaseStartedPrecedesItsTestCaseFinished() {
    string ndjson = MessagesNdjsonWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    string[] lines = SplitLines(ndjson);

    List<(int Index, string Id)> started = [];
    List<(int Index, string TestCaseStartedId)> finished = [];

    for (int i = 0; i < lines.Length; i++) {
      using JsonDocument document = JsonDocument.Parse(lines[i]);
      JsonProperty envelope = document.RootElement.EnumerateObject().First();
      if (envelope.Name == "testCaseStarted") {
        started.Add((i, envelope.Value.GetProperty("id").GetString()!));
      } else if (envelope.Name == "testCaseFinished") {
        finished.Add((i, envelope.Value.GetProperty("testCaseStartedId").GetString()!));
      }
    }

    Assert.Equal(4, started.Count);
    Assert.Equal(4, finished.Count);

    foreach ((int startIndex, string id) in started) {
      int finishIndex = finished.Single(f => f.TestCaseStartedId == id).Index;
      Assert.True(startIndex < finishIndex, $"testCaseStarted({id}) at {startIndex} must precede its testCaseFinished at {finishIndex}");
    }
  }

  [Fact]
  public void Write_CountsMatchInputScenariosFeaturesAndSteps() {
    List<ScenarioResult> results = ReportWriterTestData.BuildTwoFeatureRun();
    string ndjson = MessagesNdjsonWriter.Write(results);
    string[] lines = SplitLines(ndjson);
    List<string> envelopeNames = [.. lines.Select(EnvelopeName)];

    int distinctFeatures = results.Select(r => r.FeatureName).Distinct().Count();
    int totalSteps = results.Sum(r => r.Steps.Count);
    int totalAttachments = results.Sum(r => r.Attachments.Count + r.StateDumps.Count + (r.LogTail.Count > 0 ? 1 : 0));

    Assert.Equal(distinctFeatures, envelopeNames.Count(n => n == "source"));
    Assert.Equal(distinctFeatures, envelopeNames.Count(n => n == "gherkinDocument"));
    Assert.Equal(distinctFeatures, envelopeNames.Count(n => n == "pickle"));
    Assert.Equal(results.Count, envelopeNames.Count(n => n == "testCase"));
    Assert.Equal(results.Count, envelopeNames.Count(n => n == "testCaseStarted"));
    Assert.Equal(results.Count, envelopeNames.Count(n => n == "testCaseFinished"));
    Assert.Equal(totalSteps, envelopeNames.Count(n => n == "testStepStarted"));
    Assert.Equal(totalSteps, envelopeNames.Count(n => n == "testStepFinished"));
    Assert.Equal(totalAttachments, envelopeNames.Count(n => n == "attachment"));
  }

  [Fact]
  public void Write_NoReaderSupplied_ScreenshotAttachmentIsPlainTextPathAndNeverBase64() {
    string ndjson = MessagesNdjsonWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    string[] lines = SplitLines(ndjson);

    JsonElement screenshot = lines
        .Where(l => EnvelopeName(l) == "attachment")
        .Select(l => JsonDocument.Parse(l).RootElement.GetProperty("attachment"))
        .Single(a => a.GetProperty("body").GetString() == "/tmp/login-fail.png");

    Assert.Equal("text/x.plain", screenshot.GetProperty("mediaType").GetString());
    Assert.Equal("IDENTITY", screenshot.GetProperty("contentEncoding").GetString());
    Assert.DoesNotContain(lines, l => EnvelopeName(l) == "attachment"
        && JsonDocument.Parse(l).RootElement.GetProperty("attachment").GetProperty("contentEncoding").GetString() == "BASE64");
  }

  [Fact]
  public void Write_ReaderReturnsBytes_ScreenshotAttachmentIsBase64EncodedActualPngBytes() {
    byte[] pngMagicHeader = [137, 80, 78, 71, 13, 10, 26, 10];
    string ndjson = MessagesNdjsonWriter.Write(
        ReportWriterTestData.BuildTwoFeatureRun(),
        path => path == "/tmp/login-fail.png" ? pngMagicHeader : null);
    string[] lines = SplitLines(ndjson);

    JsonElement screenshot = lines
        .Where(l => EnvelopeName(l) == "attachment")
        .Select(l => JsonDocument.Parse(l).RootElement.GetProperty("attachment"))
        .Single(a => a.GetProperty("mediaType").GetString() == "image/png");

    Assert.Equal("BASE64", screenshot.GetProperty("contentEncoding").GetString());
    byte[] decoded = System.Convert.FromBase64String(screenshot.GetProperty("body").GetString()!);
    Assert.Equal(pngMagicHeader, decoded);
  }

  [Fact]
  public void Write_ReaderReturnsNull_FallsBackToPlainTextPathInsteadOfBrokenBase64() {
    string ndjson = MessagesNdjsonWriter.Write(
        ReportWriterTestData.BuildTwoFeatureRun(),
        _ => null);
    string[] lines = SplitLines(ndjson);

    JsonElement screenshot = lines
        .Where(l => EnvelopeName(l) == "attachment")
        .Select(l => JsonDocument.Parse(l).RootElement.GetProperty("attachment"))
        .Single(a => a.GetProperty("body").GetString() == "/tmp/login-fail.png");

    Assert.Equal("text/x.plain", screenshot.GetProperty("mediaType").GetString());
    Assert.Equal("IDENTITY", screenshot.GetProperty("contentEncoding").GetString());
  }

  [Fact]
  public void Write_TextAttachment_IsIdentityEncodedWithPlainTextMediaType() {
    string ndjson = MessagesNdjsonWriter.Write(ReportWriterTestData.BuildTwoFeatureRun());
    string[] lines = SplitLines(ndjson);

    JsonElement note = lines
        .Where(l => EnvelopeName(l) == "attachment")
        .Select(l => JsonDocument.Parse(l).RootElement.GetProperty("attachment"))
        .Single(a => a.GetProperty("body").GetString() == "some note");

    Assert.Equal("text/x.plain", note.GetProperty("mediaType").GetString());
    Assert.Equal("IDENTITY", note.GetProperty("contentEncoding").GetString());
  }

  [Fact]
  public void Write_FailureMessageWithNewlineQuoteAndBackslash_ProducesParsableLine() {
    string failureMessage = "line one\nline two \"quoted\" and a backslash \\ here";
    List<ScenarioResult> results =
    [
        new ScenarioResult(
                "escaping scenario",
                "Escaping",
                new TagSet([]),
                ScenarioOutcome.Failed,
                [new StepResult("When", "it fails", StepStatus.Failed, 10, failureMessage)],
                10) {
              FailureMessage = failureMessage,
            },
        ];

    string ndjson = MessagesNdjsonWriter.Write(results);
    string[] lines = SplitLines(ndjson);

    Assert.NotEmpty(lines);
    foreach (string line in lines) {
      JsonDocument.Parse(line).Dispose();
    }

    JsonElement testStepFinished = lines
        .Where(l => EnvelopeName(l) == "testStepFinished")
        .Select(l => JsonDocument.Parse(l).RootElement.GetProperty("testStepFinished"))
        .Single();

    string parsedMessage = testStepFinished.GetProperty("testStepResult").GetProperty("message").GetString()!;
    Assert.Equal(failureMessage, parsedMessage);
  }

  private static string[] SplitLines(string ndjson) {
    return ndjson.Split('\n');
  }

  private static string EnvelopeName(string line) {
    using JsonDocument document = JsonDocument.Parse(line);
    return document.RootElement.EnumerateObject().First().Name;
  }
}
