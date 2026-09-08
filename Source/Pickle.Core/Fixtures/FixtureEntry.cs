using System;

namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>One .rws a suite can see, and which of the two fixture directories it came from.</summary>
public class FixtureEntry {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="name">The fixture's name, without the <c>.rws</c> extension.</param>
  /// <param name="fullPath">The path to the file this entry describes.</param>
  /// <param name="isRecorded">Whether this copy was written by Save fixture rather than committed with the mod.</param>
  /// <param name="shadowedPath">The committed copy this entry hides, or <c>null</c> when it hides nothing.</param>
  /// <param name="isShadowed">Whether a recorded copy of the same name wins over this one.</param>
  /// <param name="sizeBytes">The file's size in bytes.</param>
  /// <param name="modified">When the file was last written.</param>
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

  /// <summary>The fixture's name, without the <c>.rws</c> extension.</summary>
  public string Name { get; }

  /// <summary>The path to the file this entry describes.</summary>
  public string FullPath { get; }

  /// <summary>Written by Save fixture rather than committed with the mod.</summary>
  public bool IsRecorded { get; }

  /// <summary>The committed copy this entry hides, or null when it hides nothing.</summary>
  public string? ShadowedPath { get; }

  /// <summary>A recorded copy of the same name wins over this one, so no run will load it.</summary>
  public bool IsShadowed { get; }

  /// <summary>The file's size in bytes.</summary>
  public long SizeBytes { get; }

  /// <summary>When the file was last written.</summary>
  public DateTime Modified { get; }
}
