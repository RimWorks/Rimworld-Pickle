using System;

namespace RimWorks.Pickle;

/// <summary>Marks a method to run after every scenario, or only scenarios carrying a given tag.</summary>
[AttributeUsage(AttributeTargets.Method)]
public class AfterScenarioAttribute : Attribute {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="tag">The tag a scenario must carry for this hook to run, or <c>null</c> to run after every scenario.</param>
  public AfterScenarioAttribute(string? tag = null) {
    Tag = tag;
  }

  /// <summary>The tag a scenario must carry for this hook to run, or <c>null</c> to run after every scenario.</summary>
  public string? Tag { get; }
}
