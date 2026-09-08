namespace RimWorks.Pickle.Core.Steps;

/// <summary>The outcome of matching a step's text against a <see cref="StepTable"/>: one of <see cref="MatchedStep"/>,
/// <see cref="AmbiguousStep"/> or <see cref="UndefinedStep"/>.</summary>
public abstract class StepResolution {
  // Closed union: only Matched, Ambiguous and Undefined resolve a step.
  private protected StepResolution() {
  }
}
