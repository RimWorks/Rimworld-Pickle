using System;

namespace Pickle;

[AttributeUsage(AttributeTargets.Method)]
public class ThenAttribute : Attribute {
  public ThenAttribute(string pattern) {
    Pattern = pattern;
  }

  public string Pattern { get; }
}
