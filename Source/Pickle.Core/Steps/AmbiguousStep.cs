using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Steps;

public class AmbiguousStep : StepResolution {
  public AmbiguousStep(IReadOnlyList<StepDefinition> matches) {
    Matches = matches;
  }

  public IReadOnlyList<StepDefinition> Matches { get; }
}
