using System;

namespace RimWorks.Pickle;

/// <summary>One call to <see cref="PickleContext.Assert"/>, kept for the report even when it passed.</summary>
public sealed class AssertRecord {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="passed">Whether the condition held.</param>
  /// <param name="label">The message passed to the assert, or <c>null</c> if none was given.</param>
  public AssertRecord(bool passed, string? label) {
    Passed = passed;
    Label = label;
  }

  /// <summary>Whether the condition held.</summary>
  public bool Passed { get; }

  /// <summary>The message passed to the assert, or <c>null</c> if none was given.</summary>
  public string? Label { get; }
}
