namespace RimWorks.Pickle.Core.Steps;

/// <summary>The Gherkin keyword a step definition matches. Matching itself ignores the keyword in the feature file.</summary>
public enum StepKind {
  /// <summary>A step that sets up state before the scenario acts.</summary>
  Given,

  /// <summary>A step that performs an action.</summary>
  When,

  /// <summary>A step that asserts an outcome.</summary>
  Then
}
