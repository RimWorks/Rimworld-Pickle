namespace RimWorks.Pickle.Core.Run;

/// <summary>What one step in a scenario did, and how long it took.</summary>
public class StepResult {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="keyword">The Gherkin keyword the step used, such as <c>Given</c> or <c>When</c>.</param>
  /// <param name="text">The step's text, after the keyword.</param>
  /// <param name="status">Whether the step passed, failed, or was skipped.</param>
  /// <param name="durationMs">How long the step took, in milliseconds.</param>
  /// <param name="failureMessage">Why the step failed, or <c>null</c> when it did not.</param>
  public StepResult(string keyword, string text, StepStatus status, double durationMs, string? failureMessage = null) {
    Keyword = keyword;
    Text = text;
    Status = status;
    DurationMs = durationMs;
    FailureMessage = failureMessage;
  }

  /// <summary>The Gherkin keyword the step used, such as <c>Given</c> or <c>When</c>.</summary>
  public string Keyword { get; set; }

  /// <summary>The step's text, after the keyword.</summary>
  public string Text { get; set; }

  /// <summary>Whether the step passed, failed, or was skipped.</summary>
  public StepStatus Status { get; set; }

  /// <summary>How long the step took, in milliseconds.</summary>
  public double DurationMs { get; set; }

  /// <summary>Why the step failed, or <c>null</c> when it did not.</summary>
  public string? FailureMessage { get; set; }
}
