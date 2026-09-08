using System;

namespace RimWorks.Pickle;

/// <summary>Marks a method as a step that matches a Gherkin <c>Given</c> line. The keyword in the
/// feature file is ignored; matching is on the pattern text alone.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class GivenAttribute : Attribute {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="pattern">The cucumber expression to match step text against.</param>
  public GivenAttribute(string pattern) {
    Pattern = pattern;
  }

  /// <summary>The cucumber expression this step matches step text against.</summary>
  public string Pattern { get; }

  /// <summary>
  /// Seconds this step may run before the runner fails it. Zero falls back to the scenario's
  /// <c>@timeout:</c> tag, then to 5.
  /// </summary>
  public float TimeoutSeconds { get; set; }
}
