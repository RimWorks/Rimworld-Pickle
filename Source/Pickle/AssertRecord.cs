using System;

namespace RimWorks.Pickle;

public sealed class AssertRecord {
  public AssertRecord(bool passed, string? label) {
    Passed = passed;
    Label = label;
  }

  public bool Passed { get; }

  public string? Label { get; }
}
