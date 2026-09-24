using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Core.Reports;

/// <summary>Renders a run as Cucumber's NDJSON messages format, for tools that already speak that protocol.</summary>
public static class MessagesNdjsonWriter {
  private const string MetaLine =
      "{\"meta\":{\"protocolVersion\":\"22.0.0\",\"implementation\":{\"name\":\"Pickle\",\"version\":\"1.0.0\"}}}";

  private const string Timestamp = "{\"seconds\":0,\"nanos\":0}";

  /// <summary>Builds the NDJSON message stream for a completed run, one JSON object per line.</summary>
  /// <param name="results">Every scenario the run produced, in report order.</param>
  /// <param name="readAttachmentBytes">Resolves an attachment's content to its raw bytes, used to inline a screenshot as base64. Attachments render as plain text when this is <c>null</c> or returns <c>null</c>.</param>
  /// <returns>The message stream, newline-joined.</returns>
  public static string Write(IReadOnlyList<ScenarioResult> results, Func<string, byte[]?>? readAttachmentBytes = null) {
    List<string> lines = new List<string> { MetaLine };
    int nextId = 1;

    foreach (IGrouping<string, ScenarioResult> feature in results.GroupBy(r => r.FeatureName)) {
      lines.Add(BuildSource(feature.Key));
      lines.Add(BuildGherkinDocument(feature.Key));
    }

    // A pickle is one compiled scenario, not one feature, and its steps are what every
    // testStep's pickleStepId has to resolve against.
    List<(string PickleId, List<string> StepIds)> pickles = new();
    foreach (ScenarioResult scenario in results) {
      string pickleId = (nextId++).ToString(CultureInfo.InvariantCulture);
      List<string> pickleStepIds = new();
      for (int i = 0; i < scenario.Steps.Count; i++) {
        pickleStepIds.Add((nextId++).ToString(CultureInfo.InvariantCulture));
      }

      pickles.Add((pickleId, pickleStepIds));
      lines.Add(BuildPickle(pickleId, scenario, pickleStepIds));
    }

    lines.Add(BuildTestRunStarted());

    bool anyFailed = false;
    for (int i = 0; i < results.Count; i++) {
      ScenarioResult scenario = results[i];
      anyFailed |= scenario.Outcome == ScenarioOutcome.Failed;
      AppendScenario(lines, scenario, pickles[i].PickleId, pickles[i].StepIds, ref nextId, readAttachmentBytes);
    }

    lines.Add(BuildTestRunFinished(!anyFailed));
    return string.Join("\n", lines);
  }

  private static void AppendScenario(
      List<string> lines,
      ScenarioResult scenario,
      string pickleId,
      IReadOnlyList<string> pickleStepIds,
      ref int nextId,
      Func<string, byte[]?>? readAttachmentBytes) {
    string testCaseId = (nextId++).ToString(CultureInfo.InvariantCulture);
    List<string> stepIds = new List<string>();
    for (int i = 0; i < scenario.Steps.Count; i++) {
      stepIds.Add((nextId++).ToString(CultureInfo.InvariantCulture));
    }

    lines.Add(BuildTestCase(testCaseId, pickleId, stepIds, pickleStepIds));

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

  private static string BuildSource(string featureName) {
    return $"{{\"source\":{{\"uri\":{JsonEscape.Quote(featureName)},\"data\":\"\",\"mediaType\":\"text/x.cucumber.gherkin+plain\"}}}}";
  }

  private static string BuildGherkinDocument(string featureName) {
    string uri = JsonEscape.Quote(featureName);
    return $"{{\"gherkinDocument\":{{\"uri\":{uri},\"feature\":{{\"name\":{uri}}}}}}}";
  }

  private static string BuildPickle(string pickleId, ScenarioResult scenario, IReadOnlyList<string> pickleStepIds) {
    string steps = string.Join(
        ",",
        scenario.Steps.Select((step, i) =>
            $"{{\"id\":{JsonEscape.Quote(pickleStepIds[i])},\"text\":{JsonEscape.Quote($"{step.Keyword} {step.Text}".Trim())}}}"));

    return "{\"pickle\":{\"id\":" + JsonEscape.Quote(pickleId)
        + ",\"uri\":" + JsonEscape.Quote(scenario.FeatureName)
        + ",\"name\":" + JsonEscape.Quote(scenario.ScenarioName)
        + ",\"steps\":[" + steps + "]}}";
  }

  private static string BuildTestRunStarted() {
    return $"{{\"testRunStarted\":{{\"timestamp\":{Timestamp}}}}}";
  }

  private static string BuildTestCase(
      string testCaseId, string pickleId, IReadOnlyList<string> stepIds, IReadOnlyList<string> pickleStepIds) {
    string steps = string.Join(
        ",",
        stepIds.Select((id, i) =>
            $"{{\"id\":{JsonEscape.Quote(id)},\"pickleStepId\":{JsonEscape.Quote(pickleStepIds[i])}}}"));

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
