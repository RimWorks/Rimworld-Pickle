using System.Collections.Generic;
using RimWorks.Pickle.Core.Model;

namespace RimWorks.Pickle.Core.Run;

/// <summary>What ran, what it did, and what it left behind, for one scenario. The shape every report writer reads from.</summary>
public class ScenarioResult {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="scenarioName">The scenario's name from the feature file.</param>
  /// <param name="featureName">The feature it belongs to, used to group results in reports.</param>
  /// <param name="tags">The tags on the scenario, inherited or its own.</param>
  /// <param name="outcome">Whether the scenario passed, failed, or was skipped.</param>
  /// <param name="steps">Every step that ran, in order.</param>
  /// <param name="durationMs">How long the scenario took, in milliseconds.</param>
  public ScenarioResult(
      string scenarioName,
      string featureName,
      TagSet tags,
      ScenarioOutcome outcome,
      IReadOnlyList<StepResult> steps,
      double durationMs) {
    ScenarioName = scenarioName;
    FeatureName = featureName;
    Tags = tags;
    Outcome = outcome;
    Steps = steps;
    DurationMs = durationMs;
  }

  /// <summary>The scenario's name from the feature file.</summary>
  public string ScenarioName { get; set; }

  /// <summary>The feature this scenario belongs to, used to group results in reports.</summary>
  public string FeatureName { get; set; }

  /// <summary>The tags on the scenario, inherited or its own.</summary>
  public TagSet Tags { get; set; }

  /// <summary>Whether the scenario passed, failed, or was skipped.</summary>
  public ScenarioOutcome Outcome { get; set; }

  /// <summary>Every step that ran, in order.</summary>
  public IReadOnlyList<StepResult> Steps { get; set; }

  /// <summary>How long the scenario took, in milliseconds.</summary>
  public double DurationMs { get; set; }

  /// <summary>Why the scenario failed, or <c>null</c> when it did not.</summary>
  public string? FailureMessage { get; set; }

  /// <summary>The last few log lines captured around the failure. Empty on a pass.</summary>
  public IReadOnlyList<string> LogTail { get; set; } = [];

  /// <summary>Files saved alongside the result, such as screenshots.</summary>
  public IReadOnlyList<(string Name, string Content)> Attachments { get; set; } = [];

  /// <summary>State dumps captured for debugging a failure, tagged with where each one came from.</summary>
  public IReadOnlyList<(string Source, string Content)> StateDumps { get; set; } = [];

  /// <summary>What the scenario's ticks cost, or null when it drove no ticks.</summary>
  public (int Ticks, double MeanMs, double MaxMs)? TickCost { get; set; }

  /// <summary>How many times the scenario ran. Above one means a retry was spent on it.</summary>
  public int Attempts { get; set; } = 1;

  /// <summary>What each earlier attempt failed with, oldest first. Empty unless retried.</summary>
  public IReadOnlyList<(int Attempt, string? Message)> FailedAttempts { get; set; } = [];
}
