using System.Collections.Generic;

namespace Pickle.Core.Steps;

public class MatchedStep : StepResolution {
  public MatchedStep(StepDefinition definition, IReadOnlyList<object?> args) {
    Definition = definition;
    Args = args;
  }

  public StepDefinition Definition { get; }

  public IReadOnlyList<object?> Args { get; }
}
