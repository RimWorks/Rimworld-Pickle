namespace RimWorks.Pickle.Core.Run;

/// <summary>How a scenario finished.</summary>
public enum ScenarioOutcome {
  /// <summary>Every step passed.</summary>
  Passed,

  /// <summary>A step failed, was undefined, or was ambiguous.</summary>
  Failed,

  /// <summary>Every step in the scenario was skipped.</summary>
  Skipped,
}
