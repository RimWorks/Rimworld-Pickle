using System;

namespace RimWorks.Pickle;

[AttributeUsage(AttributeTargets.Method)]
public class ThenAttribute : Attribute {
  public ThenAttribute(string pattern) {
    Pattern = pattern;
  }

  public string Pattern { get; }

  /// <summary>
  /// Seconds this step may run before the runner fails it. Zero uses the run default of 5.
  /// </summary>
  public float TimeoutSeconds { get; set; }
}
