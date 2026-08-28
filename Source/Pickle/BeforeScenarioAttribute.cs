using System;

namespace Pickle;

[AttributeUsage(AttributeTargets.Method)]
public class BeforeScenarioAttribute : Attribute {
  public BeforeScenarioAttribute(string? tag = null) {
    Tag = tag;
  }

  public string? Tag { get; }
}
