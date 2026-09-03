using System;

namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>One .rws a suite can see, and which of the two fixture directories it came from.</summary>
public class FixtureEntry {
  public FixtureEntry(
      string name, string fullPath, bool isRecorded, string? shadowedPath, bool isShadowed, long sizeBytes, DateTime modified) {
    Name = name;
    FullPath = fullPath;
    IsRecorded = isRecorded;
    ShadowedPath = shadowedPath;
    IsShadowed = isShadowed;
    SizeBytes = sizeBytes;
    Modified = modified;
  }

  public string Name { get; }

  public string FullPath { get; }

  /// <summary>Written by Save fixture rather than committed with the mod.</summary>
  public bool IsRecorded { get; }

  /// <summary>The committed copy this entry hides, or null when it hides nothing.</summary>
  public string? ShadowedPath { get; }

  /// <summary>A recorded copy of the same name wins over this one, so no run will load it.</summary>
  public bool IsShadowed { get; }

  public long SizeBytes { get; }

  public DateTime Modified { get; }
}
