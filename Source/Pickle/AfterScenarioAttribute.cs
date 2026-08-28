using System;

namespace Pickle;

[AttributeUsage(AttributeTargets.Method)]
public class AfterScenarioAttribute : Attribute {
  public AfterScenarioAttribute(string? tag = null) {
    Tag = tag;
  }

  public string? Tag { get; }
}
