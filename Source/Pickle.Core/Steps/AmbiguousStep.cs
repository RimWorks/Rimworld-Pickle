using System.Collections.Generic;

namespace Pickle.Core.Steps;

public class AmbiguousStep : StepResolution {
  public AmbiguousStep(IReadOnlyList<StepDefinition> matches) {
    Matches = matches;
  }

  public IReadOnlyList<StepDefinition> Matches { get; }
}
