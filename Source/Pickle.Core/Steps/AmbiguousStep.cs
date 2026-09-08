using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Steps;

/// <summary>A step whose text matched more than one <see cref="StepDefinition"/>, which the runner cannot resolve on its own.</summary>
public class AmbiguousStep : StepResolution {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="matches">Every step definition that matched the step text.</param>
  public AmbiguousStep(IReadOnlyList<StepDefinition> matches) {
    Matches = matches;
  }

  /// <summary>Every step definition that matched the step text.</summary>
  public IReadOnlyList<StepDefinition> Matches { get; }
}
