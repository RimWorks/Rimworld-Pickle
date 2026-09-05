using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Core.Reports;

/// <summary>
/// Fills the report bundle with a run's results, replacing a placeholder script tag.
/// The output opens from disk with nothing to fetch.
/// </summary>
public static class HtmlReportWriter {
  private const string Placeholder = "__PICKLE_REPORT_JSON__";

  public static string Write(
      IReadOnlyList<ScenarioResult> results,
      string exitReason,
      string template,
      Func<string, byte[]?>? readAttachmentBytes = null,
      string? stringsJson = null,
      string? setName = null) {
    return template.Replace(Placeholder, BuildPayload(results, exitReason, readAttachmentBytes, stringsJson, setName));
  }

  public static string BuildPayload(
      IReadOnlyList<ScenarioResult> results,
      string exitReason,
      Func<string, byte[]?>? readAttachmentBytes,
      string? stringsJson = null,
      string? setName = null) {
    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"status\":\"idle\",\"feature\":\"\",\"scenario\":\"\",\"step\":\"\",");
    json.Append("\"passed\":").Append(results.Count(r => r.Outcome == ScenarioOutcome.Passed)).Append(',');
    json.Append("\"failed\":").Append(results.Count(r => r.Outcome == ScenarioOutcome.Failed)).Append(',');
    json.Append("\"cancelRequested\":false,\"watch\":true,\"breakOnFailure\":false,\"controllable\":false,");
    json.Append("\"exitReason\":").Append(JsonEscape.Quote(exitReason)).Append(',');
    json.Append("\"setName\":").Append(setName == null ? "null" : JsonEscape.Quote(setName)).Append(',');
    json.Append("\"strings\":").Append(stringsJson ?? "{}").Append(',');

    // The dashboard groups by feature, but a ScenarioResult only knows its feature
    // name, so that name is both the grouping key and the stand-in for a path.
    List<string> features = new List<string>();
    int index = 0;
    foreach (IGrouping<string, ScenarioResult> group in results.GroupBy(r => r.FeatureName)) {
      List<string> scenarios = new List<string>();
      foreach (ScenarioResult scenario in group) {
        scenarios.Add(BuildScenario(scenario, index, readAttachmentBytes));
        index++;
      }

      StringBuilder feature = new StringBuilder();
      feature.Append('{');
      feature.Append("\"name\":").Append(JsonEscape.Quote(group.Key)).Append(',');
      feature.Append("\"mod\":\"\",");
      feature.Append("\"path\":").Append(JsonEscape.Quote(group.Key)).Append(',');
      feature.Append("\"tags\":[],");
      feature.Append("\"scenarios\":[").Append(string.Join(",", scenarios)).Append(']');
      feature.Append('}');
      features.Add(feature.ToString());
    }

    json.Append("\"features\":[").Append(string.Join(",", features)).Append(']');
    json.Append('}');

    // A failure message containing "</script>" would otherwise close the tag the
    // payload lives in and break the page.
    return json.ToString().Replace("</", "<\\/");
  }

  private static string BuildScenario(
      ScenarioResult scenario,
      int index,
      Func<string, byte[]?>? readAttachmentBytes) {
    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"name\":").Append(JsonEscape.Quote(scenario.ScenarioName)).Append(',');
    json.Append("\"index\":").Append(index).Append(',');
    json.Append("\"selected\":true,\"line\":0,");
    json.Append("\"tags\":[").Append(string.Join(",", scenario.Tags.Select(JsonEscape.Quote))).Append("],");
    json.Append("\"outcome\":").Append(JsonEscape.Quote(scenario.Outcome.ToString())).Append(',');
    json.Append("\"durationMs\":").Append(scenario.DurationMs.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
    json.Append("\"attempts\":").Append(scenario.Attempts).Append(',');
    json.Append("\"tickCost\":").Append(BuildTickCost(scenario.TickCost)).Append(',');
    json.Append("\"failedAttempts\":[")
        .Append(string.Join(",", scenario.FailedAttempts.Select(BuildFailedAttempt)))
        .Append("],");
    json.Append("\"failureMessage\":").Append(Quote(scenario.FailureMessage)).Append(',');
    json.Append("\"logTail\":[").Append(string.Join(",", scenario.LogTail.Select(JsonEscape.Quote))).Append("],");
    json.Append("\"attachments\":[")
        .Append(string.Join(",", EvidenceAttachments.Expand(scenario.Attachments).Select(a => BuildAttachment(a, readAttachmentBytes))))
        .Append("],");
    json.Append("\"stateDumps\":[")
        .Append(string.Join(",", scenario.StateDumps.Select(BuildStateDump)))
        .Append("],");
    json.Append("\"steps\":[").Append(string.Join(",", scenario.Steps.Select(BuildStep))).Append(']');
    json.Append('}');
    return json.ToString();
  }

  private static string BuildAttachment(
      (string Name, string Content) attachment,
      Func<string, byte[]?>? readAttachmentBytes) {
    // images inline as data URIs so the report stands alone. film frames stay linked:
    // base64 of a whole strip pushes the file past what a browser will open.
    if (attachment.Name == "film-frames" || attachment.Name == "film-video") {
      string dir = Path.GetDirectoryName(attachment.Content) ?? string.Empty;
      string folder = Path.GetFileName(dir);

      if (attachment.Name == "film-video") {
        byte[]? encoded = readAttachmentBytes?.Invoke(attachment.Content);
        string source = encoded != null
            ? "data:video/webm;base64," + Convert.ToBase64String(encoded)
            : $"screenshots/film/{folder}/film.webm";

        return "{\"name\":\"film-video\",\"content\":" + JsonEscape.Quote(source) + "}";
      }

      return "{\"name\":\"film-frames\",\"content\":"
          + JsonEscape.Quote($"screenshots/film/{folder}/{Path.GetFileName(attachment.Content)}") + "}";
    }

    string content = attachment.Content;
    byte[]? bytes = readAttachmentBytes?.Invoke(attachment.Content);
    if (bytes != null) {
      content = "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    return "{\"name\":" + JsonEscape.Quote(attachment.Name) + ",\"content\":" + JsonEscape.Quote(content) + "}";
  }

  private static string BuildTickCost((int Ticks, double MeanMs, double MaxMs)? cost) {
    if (!cost.HasValue) {
      return "null";
    }

    (int ticks, double meanMs, double maxMs) = cost.Value;
    return "{\"ticks\":" + ticks.ToString(CultureInfo.InvariantCulture)
        + ",\"meanMs\":" + meanMs.ToString("0.###", CultureInfo.InvariantCulture)
        + ",\"maxMs\":" + maxMs.ToString("0.###", CultureInfo.InvariantCulture) + "}";
  }

  private static string BuildFailedAttempt((int Attempt, string? Message) attempt) {
    return "{\"attempt\":" + attempt.Attempt.ToString(CultureInfo.InvariantCulture)
        + ",\"message\":" + Quote(attempt.Message) + "}";
  }

  private static string BuildStateDump((string Source, string Content) dump) {
    return "{\"source\":" + JsonEscape.Quote(dump.Source) + ",\"content\":" + JsonEscape.Quote(dump.Content) + "}";
  }

  private static string BuildStep(StepResult step) {
    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"keyword\":").Append(JsonEscape.Quote(step.Keyword.Trim())).Append(',');
    json.Append("\"text\":").Append(JsonEscape.Quote(step.Text)).Append(',');
    json.Append("\"status\":").Append(JsonEscape.Quote(step.Status.ToString())).Append(',');
    json.Append("\"durationMs\":").Append(step.DurationMs.ToString("0.##", CultureInfo.InvariantCulture)).Append(',');
    json.Append("\"failureMessage\":").Append(Quote(step.FailureMessage));
    json.Append('}');
    return json.ToString();
  }

  private static string Quote(string? value) {
    return value == null ? "null" : JsonEscape.Quote(value);
  }
}
