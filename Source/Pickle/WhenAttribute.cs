using System;

namespace Pickle;

[AttributeUsage(AttributeTargets.Method)]
public class WhenAttribute : Attribute {
  public WhenAttribute(string pattern) {
    Pattern = pattern;
  }

  public string Pattern { get; }
}
