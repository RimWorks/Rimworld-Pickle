using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Pickle.Core.Run;

namespace Pickle.Core.Reports;

public static class MessagesNdjsonWriter {
  private const string Timestamp = "{\"seconds\":0,\"nanos\":0}";

  public static string Write(IReadOnlyList<ScenarioResult> results, Func<string, byte[]?>? readAttachmentBytes = null) {
    List<string> lines = new List<string> { BuildMeta() };
    int nextId = 1;

    Dictionary<string, string> pickleIdsByFeature = new Dictionary<string, string>();
    foreach (IGrouping<string, ScenarioResult> feature in results.GroupBy(r => r.FeatureName)) {
      string pickleId = (nextId++).ToString(CultureInfo.InvariantCulture);
      pickleIdsByFeature[feature.Key] = pickleId;
      lines.Add(BuildSource(feature.Key));
      lines.Add(BuildGherkinDocument(feature.Key));
      lines.Add(BuildPickle(pickleId, feature.Key));
    }

    lines.Add(BuildTestRunStarted());

    bool anyFailed = false;
    foreach (ScenarioResult scenario in results) {
      anyFailed |= scenario.Outcome == ScenarioOutcome.Failed;
      AppendScenario(lines, scenario, pickleIdsByFeature[scenario.FeatureName], ref nextId, readAttachmentBytes);
    }

    lines.Add(BuildTestRunFinished(!anyFailed));
    return string.Join("\n", lines);
  }

  private static void AppendScenario(
      List<string> lines,
      ScenarioResult scenario,
      string pickleId,
      ref int nextId,
      Func<string, byte[]?>? readAttachmentBytes) {
    string testCaseId = (nextId++).ToString(CultureInfo.InvariantCulture);
    List<string> stepIds = new List<string>();
    for (int i = 0; i < scenario.Steps.Count; i++) {
      stepIds.Add((nextId++).ToString(CultureInfo.InvariantCulture));
    }

    lines.Add(BuildTestCase(testCaseId, pickleId, stepIds));

    string testCaseStartedId = (nextId++).ToString(CultureInfo.InvariantCulture);
    lines.Add(BuildTestCaseStarted(testCaseStartedId, testCaseId));

    for (int i = 0; i < scenario.Steps.Count; i++) {
      lines.Add(BuildTestStepStarted(testCaseStartedId, stepIds[i]));
      lines.Add(BuildTestStepFinished(testCaseStartedId, stepIds[i], scenario.Steps[i]));
    }

    foreach ((string name, string content) in scenario.Attachments) {
      lines.Add(BuildAttachmentEnvelope(testCaseStartedId, name, content, readAttachmentBytes));
    }

    foreach ((string source, string content) in scenario.StateDumps) {
      lines.Add(BuildAttachment(testCaseStartedId, "text/x.plain", "IDENTITY", $"[{source}] {content}"));
    }

    if (scenario.LogTail.Count > 0) {
      lines.Add(BuildAttachment(testCaseStartedId, "text/x.plain", "IDENTITY", string.Join("\n", scenario.LogTail)));
    }

    lines.Add(BuildTestCaseFinished(testCaseStartedId));
  }

  private static string BuildMeta() {
    return "{\"meta\":{\"protocolVersion\":\"22.0.0\",\"implementation\":{\"name\":\"Pickle\",\"version\":\"1.0.0\"}}}";
  }

  private static string BuildSource(string featureName) {
    return $"{{\"source\":{{\"uri\":{JsonEscape.Quote(featureName)},\"data\":\"\",\"mediaType\":\"text/x.cucumber.gherkin+plain\"}}}}";
  }

  private static string BuildGherkinDocument(string featureName) {
    string uri = JsonEscape.Quote(featureName);
    return $"{{\"gherkinDocument\":{{\"uri\":{uri},\"feature\":{{\"name\":{uri}}}}}}}";
  }

  private static string BuildPickle(string pickleId, string featureName) {
    string uri = JsonEscape.Quote(featureName);
    return $"{{\"pickle\":{{\"id\":{JsonEscape.Quote(pickleId)},\"uri\":{uri},\"name\":{uri}}}}}";
  }

  private static string BuildTestRunStarted() {
    return $"{{\"testRunStarted\":{{\"timestamp\":{Timestamp}}}}}";
  }

  private static string BuildTestCase(string testCaseId, string pickleId, IReadOnlyList<string> stepIds) {
    string steps = string.Join(",", stepIds.Select(id => $"{{\"id\":{JsonEscape.Quote(id)},\"pickleStepId\":{JsonEscape.Quote(id)}}}"));
    return $"{{\"testCase\":{{\"id\":{JsonEscape.Quote(testCaseId)},\"pickleId\":{JsonEscape.Quote(pickleId)},\"testSteps\":[{steps}]}}}}";
  }

  private static string BuildTestCaseStarted(string testCaseStartedId, string testCaseId) {
    return $"{{\"testCaseStarted\":{{\"id\":{JsonEscape.Quote(testCaseStartedId)},\"testCaseId\":{JsonEscape.Quote(testCaseId)},\"timestamp\":{Timestamp}}}}}";
  }

  private static string BuildTestStepStarted(string testCaseStartedId, string testStepId) {
    return $"{{\"testStepStarted\":{{\"testCaseStartedId\":{JsonEscape.Quote(testCaseStartedId)},\"testStepId\":{JsonEscape.Quote(testStepId)},\"timestamp\":{Timestamp}}}}}";
  }

  private static string BuildTestStepFinished(string testCaseStartedId, string testStepId, StepResult step) {
    long seconds = (long)(step.DurationMs / 1000.0);
    int nanos = (int)((step.DurationMs - (seconds * 1000)) * 1_000_000);
    string duration = $"{{\"seconds\":{seconds},\"nanos\":{nanos}}}";
    string message = step.FailureMessage != null ? $",\"message\":{JsonEscape.Quote(step.FailureMessage)}" : string.Empty;
    string status = MapStepStatus(step.Status);

    return "{\"testStepFinished\":{\"testCaseStartedId\":" + JsonEscape.Quote(testCaseStartedId)
        + ",\"testStepId\":" + JsonEscape.Quote(testStepId)
        + ",\"testStepResult\":{\"status\":" + JsonEscape.Quote(status) + ",\"duration\":" + duration + message + "}"
        + ",\"timestamp\":" + Timestamp + "}}";
  }

  private static string BuildAttachmentEnvelope(
      string testCaseStartedId,
      string name,
      string content,
      Func<string, byte[]?>? readAttachmentBytes) {
    if (name == "screenshot") {
      byte[]? bytes = readAttachmentBytes?.Invoke(content);
      if (bytes != null) {
        return BuildAttachment(testCaseStartedId, "image/png", "BASE64", Convert.ToBase64String(bytes));
      }
    }

    return BuildAttachment(testCaseStartedId, "text/x.plain", "IDENTITY", content);
  }

  private static string BuildAttachment(string testCaseStartedId, string mediaType, string contentEncoding, string body) {
    return "{\"attachment\":{\"testCaseStartedId\":" + JsonEscape.Quote(testCaseStartedId)
        + ",\"body\":" + JsonEscape.Quote(body)
        + ",\"mediaType\":" + JsonEscape.Quote(mediaType)
        + ",\"contentEncoding\":" + JsonEscape.Quote(contentEncoding) + "}}";
  }

  private static string BuildTestCaseFinished(string testCaseStartedId) {
    return $"{{\"testCaseFinished\":{{\"testCaseStartedId\":{JsonEscape.Quote(testCaseStartedId)},\"timestamp\":{Timestamp}}}}}";
  }

  private static string BuildTestRunFinished(bool success) {
    return $"{{\"testRunFinished\":{{\"success\":{(success ? "true" : "false")},\"timestamp\":{Timestamp}}}}}";
  }

  private static string MapStepStatus(StepStatus status) {
    return status switch {
      StepStatus.Passed => "PASSED",
      StepStatus.Failed => "FAILED",
      StepStatus.Skipped => "SKIPPED",
      StepStatus.Undefined => "UNDEFINED",
      StepStatus.Ambiguous => "AMBIGUOUS",
      _ => "UNKNOWN",
    };
  }
}
