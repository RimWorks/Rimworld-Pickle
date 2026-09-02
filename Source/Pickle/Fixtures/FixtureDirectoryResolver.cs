using System;
using System.IO;
using Verse;

namespace RimWorks.Pickle.Fixtures;

/// <summary>
/// Picks the root a recorded fixture is written under. The mod folder is read-only on a
/// Workshop install and under Docker, so writing the fixture where it will eventually be
/// committed is not something Pickle can count on.
/// </summary>
public static class FixtureDirectoryResolver {
  private const string ArgName = "-pickle-fixtures-dir";

  private static string? resolved;

  /// <summary>Null when the mod folders are writable, which leaves the committed layout alone.</summary>
  public static string? Resolve() {
    if (resolved != null) {
      return resolved;
    }

    if (GenCommandLine.TryGetCommandLineArg(ArgName, out string explicitDir) && !explicitDir.NullOrEmpty()) {
      resolved = explicitDir;
      return resolved;
    }

    try {
      resolved = Path.Combine(GenFilePaths.SaveDataFolderPath, "PickleFixtures");
    } catch (Exception) {
      resolved = Path.Combine(Directory.GetCurrentDirectory(), "pickle-fixtures");
    }

    return resolved;
  }
}
