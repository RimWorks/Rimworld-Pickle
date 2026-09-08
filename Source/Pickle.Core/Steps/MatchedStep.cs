using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Steps;

/// <summary>A step that matched exactly one <see cref="StepDefinition"/>, with its arguments already converted.</summary>
public class MatchedStep : StepResolution {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="definition">The step definition that matched.</param>
  /// <param name="args">The captured groups, converted to <see cref="StepDefinition.ParameterTypes"/>, in order.</param>
  public MatchedStep(StepDefinition definition, IReadOnlyList<object?> args) {
    Definition = definition;
    Args = args;
  }

  /// <summary>The step definition that matched.</summary>
  public StepDefinition Definition { get; }

  /// <summary>The captured groups, converted to <see cref="StepDefinition.ParameterTypes"/>, in order.</summary>
  public IReadOnlyList<object?> Args { get; }
}
