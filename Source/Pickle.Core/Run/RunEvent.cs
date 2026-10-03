using System.Collections.Generic;
using System.Globalization;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Steps;

namespace RimWorks.Pickle.Core.Run;

/// <summary>
/// Builds the newline delimited JSON a streaming run writes, one object per line. Split from the
/// server so the wire format stays unit testable.
/// </summary>
public static class RunEvent {
  /// <summary>Announces the feature about to run and how many scenarios it holds.</summary>
  /// <param name="feature">The feature's name.</param>
  /// <param name="scenarios">How many scenarios will run.</param>
  /// <returns>One JSON object, with no trailing newline.</returns>
  public static string RunStarted(string? feature, int scenarios) {
    return "{\"event\":\"run-started\",\"feature\":" + Text(feature)
        + ",\"scenarios\":" + Count(scenarios) + "}";
  }

  /// <summary>Announces a scenario starting and how many steps it holds.</summary>
  /// <param name="index">The scenario's position in the feature, counting from zero.</param>
  /// <param name="name">The scenario's name.</param>
  /// <param name="steps">How many steps it holds.</param>
  /// <returns>One JSON object, with no trailing newline.</returns>
  public static string ScenarioStarted(int index, string? name, int steps) {
    return "{\"event\":\"scenario-started\",\"index\":" + Count(index)
        + ",\"name\":" + Text(name)
        + ",\"steps\":" + Count(steps) + "}";
  }

  /// <summary>Reports one finished step.</summary>
  /// <param name="scenario">The owning scenario's index.</param>
  /// <param name="index">The step's position in the scenario, counting from zero.</param>
  /// <param name="step">The finished step.</param>
  /// <returns>One JSON object, with no trailing newline.</returns>
  public static string Step(int scenario, int index, StepResult step) {
    return "{\"event\":\"step\",\"scenario\":" + Count(scenario)
        + ",\"index\":" + Count(index)
        + ",\"keyword\":" + Text(step.Keyword)
        + ",\"text\":" + Text(step.Text)
        + ",\"status\":" + Text(step.Status.ToString())
        + ",\"durationMs\":" + Count((long)step.DurationMs)
        + ",\"args\":" + JsonEscape.Spans(step.ArgumentSpans)
        + ",\"failureMessage\":" + Nullable(step.FailureMessage) + "}";
  }

  /// <summary>Reports one finished scenario.</summary>
  /// <param name="index">The scenario's position in the feature, counting from zero.</param>
  /// <param name="name">The scenario's name.</param>
  /// <param name="outcome">The scenario's outcome name.</param>
  /// <param name="durationMs">How long the scenario took.</param>
  /// <param name="failureMessage">The failure text, or <c>null</c> when it passed.</param>
  /// <returns>One JSON object, with no trailing newline.</returns>
  public static string Scenario(
      int index,
      string? name,
      string? outcome,
      long durationMs,
      string? failureMessage) {
    return "{\"event\":\"scenario\",\"index\":" + Count(index)
        + ",\"name\":" + Text(name)
        + ",\"outcome\":" + Text(outcome)
        + ",\"durationMs\":" + Count(durationMs)
        + ",\"failureMessage\":" + Nullable(failureMessage) + "}";
  }

  /// <summary>Closes the stream with the run's tally.</summary>
  /// <param name="passed">How many scenarios passed.</param>
  /// <param name="failed">How many scenarios failed.</param>
  /// <returns>One JSON object, with no trailing newline.</returns>
  public static string RunFinished(int passed, int failed) {
    return "{\"event\":\"run-finished\",\"passed\":" + Count(passed)
        + ",\"failed\":" + Count(failed) + "}";
  }

  /// <summary>Reports a failure that ended the run, such as Gherkin that would not parse.</summary>
  /// <param name="message">What went wrong.</param>
  /// <returns>One JSON object, with no trailing newline.</returns>
  public static string Error(string? message) {
    return "{\"event\":\"error\",\"message\":" + Text(message) + "}";
  }

  /// <summary>Joins lines into a newline delimited document, with a trailing newline.</summary>
  /// <param name="lines">The lines to join.</param>
  /// <returns>Every line followed by a newline, or an empty string when there are none.</returns>
  public static string Document(IEnumerable<string> lines) {
    System.Text.StringBuilder builder = new System.Text.StringBuilder();
    foreach (string line in lines) {
      builder.Append(line).Append('\n');
    }

    return builder.ToString();
  }

  private static string Text(string? value) {
    return JsonEscape.Quote(value ?? string.Empty);
  }

  private static string Nullable(string? value) {
    return value == null ? "null" : JsonEscape.Quote(value);
  }

  private static string Count(long value) {
    return value.ToString(CultureInfo.InvariantCulture);
  }
}
