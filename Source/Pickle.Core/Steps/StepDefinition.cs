using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Steps;

/// <summary>One <c>[Given]</c>, <c>[When]</c> or <c>[Then]</c> step registered in a <see cref="StepTable"/>.</summary>
public class StepDefinition {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="pattern">The cucumber expression or, when it starts with <c>^</c>, a raw regex to match step text against.</param>
  /// <param name="kind">Whether this is a <c>Given</c>, <c>When</c> or <c>Then</c> step.</param>
  /// <param name="source">A display string naming where the step came from, for the catalogue and ambiguity messages.</param>
  /// <param name="parameterTypes">The types the captured groups in <paramref name="pattern"/> convert to, in order.</param>
  /// <param name="binding">The method or delegate that runs the step, or <c>null</c> when only cataloguing the pattern.</param>
  /// <param name="timeoutSeconds">A per-step timeout override, or <c>null</c> to use the runner default.</param>
  public StepDefinition(string pattern, StepKind kind, string source, IReadOnlyList<Type> parameterTypes, object? binding = null, float? timeoutSeconds = null) {
    Pattern = pattern;
    Kind = kind;
    Source = source;
    ParameterTypes = parameterTypes;
    Binding = binding;
    TimeoutSeconds = timeoutSeconds;
  }

  /// <summary>The cucumber expression or regex that step text is matched against.</summary>
  public string Pattern { get; }

  /// <summary>Whether this is a <c>Given</c>, <c>When</c> or <c>Then</c> step.</summary>
  public StepKind Kind { get; }

  /// <summary>Where the step came from, such as <c>ClassName.MethodName (AssemblyName)</c> or <c>Pickle engine</c>.</summary>
  public string Source { get; }

  /// <summary>The types the captured groups convert to, in the order they appear in <see cref="Pattern"/>.</summary>
  public IReadOnlyList<Type> ParameterTypes { get; }

  /// <summary>The method or delegate that runs the step, or <c>null</c> when the definition only exists for the catalogue.</summary>
  public object? Binding { get; }

  /// <summary>A per-step timeout override, or <c>null</c> to use the runner default.</summary>
  public float? TimeoutSeconds { get; }
}
