namespace RimWorks.Pickle.Core.Run;

public class StepResult {
  public StepResult(string keyword, string text, StepStatus status, double durationMs, string? failureMessage = null) {
    Keyword = keyword;
    Text = text;
    Status = status;
    DurationMs = durationMs;
    FailureMessage = failureMessage;
  }

  public string Keyword { get; set; }

  public string Text { get; set; }

  public StepStatus Status { get; set; }

  public double DurationMs { get; set; }

  public string? FailureMessage { get; set; }
}
