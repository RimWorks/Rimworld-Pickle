using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Steps;

public class StepDefinition {
  public StepDefinition(string pattern, StepKind kind, string source, IReadOnlyList<Type> parameterTypes, object? binding = null, float? timeoutSeconds = null) {
    Pattern = pattern;
    Kind = kind;
    Source = source;
    ParameterTypes = parameterTypes;
    Binding = binding;
    TimeoutSeconds = timeoutSeconds;
  }

  public string Pattern { get; }

  public StepKind Kind { get; }

  public string Source { get; }

  public IReadOnlyList<Type> ParameterTypes { get; }

  public object? Binding { get; }

  public float? TimeoutSeconds { get; }
}
