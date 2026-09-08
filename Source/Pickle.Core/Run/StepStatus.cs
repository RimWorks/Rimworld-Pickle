namespace RimWorks.Pickle.Core.Run;

/// <summary>How a single step in a scenario finished.</summary>
public enum StepStatus {
  /// <summary>The step ran and its assertions held.</summary>
  Passed,

  /// <summary>The step ran and an assertion or the step body threw.</summary>
  Failed,

  /// <summary>The step never ran because an earlier step in the scenario already failed.</summary>
  Skipped,

  /// <summary>No step definition matched the step's text.</summary>
  Undefined,

  /// <summary>More than one step definition matched the step's text.</summary>
  Ambiguous,
}
