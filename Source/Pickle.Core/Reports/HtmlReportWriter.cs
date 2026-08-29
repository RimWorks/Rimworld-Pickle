using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using Pickle.Core.Run;

namespace Pickle.Core.Reports;

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
      string? stringsJson = null) {
    return template.Replace(Placeholder, BuildPayload(results, exitReason, readAttachmentBytes, stringsJson));
  }

  public static string BuildPayload(
      IReadOnlyList<ScenarioResult> results,
      string exitReason,
      Func<string, byte[]?>? readAttachmentBytes,
      string? stringsJson = null) {
    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"status\":\"idle\",\"feature\":\"\",\"scenario\":\"\",\"step\":\"\",");
    json.Append("\"passed\":").Append(results.Count(r => r.Outcome == ScenarioOutcome.Passed)).Append(',');
    json.Append("\"failed\":").Append(results.Count(r => r.Outcome == ScenarioOutcome.Failed)).Append(',');
    json.Append("\"cancelRequested\":false,\"watch\":true,\"breakOnFailure\":false,\"controllable\":false,");
    json.Append("\"exitReason\":").Append(JsonEscape.Quote(exitReason)).Append(',');
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
    json.Append("\"failureMessage\":").Append(Quote(scenario.FailureMessage)).Append(',');
    json.Append("\"logTail\":[").Append(string.Join(",", scenario.LogTail.Select(JsonEscape.Quote))).Append("],");
    json.Append("\"attachments\":[")
        .Append(string.Join(",", scenario.Attachments.Select(a => BuildAttachment(a, readAttachmentBytes))))
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
    // Attachment content is a path on disk. The report has to stand alone, so an
    // image becomes a data URI and anything unreadable falls back to the path.
    // Film frames are linked, not inlined. A strip is dozens of full size frames, and
    // base64 would push the report past what a browser will happily open.
    // film-frames points at frame zero. The video is encoded after the run, so the
    // report looks for it beside that frame rather than expecting an attachment.
    if (attachment.Name == "film-frames") {
      string dir = Path.GetDirectoryName(attachment.Content) ?? string.Empty;
      string folder = Path.GetFileName(dir);
      string name = File.Exists(Path.Combine(dir, "film.webm")) ? "film-video" : "film-frames";
      string file = name == "film-video" ? "film.webm" : Path.GetFileName(attachment.Content);
      return "{\"name\":" + JsonEscape.Quote(name)
          + ",\"content\":" + JsonEscape.Quote($"screenshots/film/{folder}/{file}") + "}";
    }

    string content = attachment.Content;
    byte[]? bytes = readAttachmentBytes?.Invoke(attachment.Content);
    if (bytes != null) {
      content = "data:image/png;base64," + Convert.ToBase64String(bytes);
    }

    return "{\"name\":" + JsonEscape.Quote(attachment.Name) + ",\"content\":" + JsonEscape.Quote(content) + "}";
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
