using System;

namespace Pickle;

[AttributeUsage(AttributeTargets.Method)]
public class GivenAttribute : Attribute {
  public GivenAttribute(string pattern) {
    Pattern = pattern;
  }

  public string Pattern { get; }
}
