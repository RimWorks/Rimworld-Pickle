using System;
using System.Collections.Generic;
using Pickle.Core.Model;

namespace Pickle.Core.Run;

public class ScenarioResult {
  public ScenarioResult(
      string scenarioName,
      string featureName,
      TagSet tags,
      ScenarioOutcome outcome,
      IReadOnlyList<StepResult> steps,
      double durationMs,
      string? failureMessage = null,
      IReadOnlyList<string>? logTail = null,
      IReadOnlyList<(string Name, string Content)>? attachments = null,
      IReadOnlyList<(string Source, string Content)>? stateDumps = null) {
    ScenarioName = scenarioName;
    FeatureName = featureName;
    Tags = tags;
    Outcome = outcome;
    Steps = steps;
    DurationMs = durationMs;
    FailureMessage = failureMessage;
    LogTail = logTail ?? Array.Empty<string>();
    Attachments = attachments ?? Array.Empty<(string, string)>();
    StateDumps = stateDumps ?? Array.Empty<(string, string)>();
  }

  public string ScenarioName { get; set; }

  public string FeatureName { get; set; }

  public TagSet Tags { get; set; }

  public ScenarioOutcome Outcome { get; set; }

  public IReadOnlyList<StepResult> Steps { get; set; }

  public double DurationMs { get; set; }

  public string? FailureMessage { get; set; }

  public IReadOnlyList<string> LogTail { get; set; }

  public IReadOnlyList<(string Name, string Content)> Attachments { get; set; }

  public IReadOnlyList<(string Source, string Content)> StateDumps { get; set; }
}
