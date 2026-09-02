using System;
using System.Collections.Generic;
using System.IO;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Fixtures;

/// <summary>
/// Picks the root a recorded fixture is written under. A Workshop install and a Docker
/// container both mount the mod folder read-only, so a fixture cannot always be written
/// where it will eventually be committed.
/// </summary>
public static class FixtureDirectoryResolver {
  private const string ArgName = "-pickle-fixtures-dir";

  private static readonly Dictionary<string, string?> ByModRoot = new(StringComparer.Ordinal);

  /// <summary>
  /// Null when the mod folders are writable, which leaves the committed layout alone.
  /// </summary>
  /// <param name="modRoot">The mod folder to test. Probed and cached per root.</param>
  /// <returns>The writable root, or null to write into each mod's own Pickle/Fixtures/.</returns>
  public static string? Resolve(string modRoot) {
    if (ByModRoot.TryGetValue(modRoot, out string? cached)) {
      return cached;
    }

    string? answer = Compute(modRoot);
    ByModRoot[modRoot] = answer;
    return answer;
  }

  // Per mod root, not once for the install: local Mods, the Workshop content directory and a
  // container bind mount are three different filesystems with three different answers.
  private static string? Compute(string modRoot) {
    if (GenCommandLine.TryGetCommandLineArg(ArgName, out string explicitDir) && !explicitDir.NullOrEmpty()) {
      return explicitDir;
    }

    if (IsWritable(Path.Combine(modRoot, "Pickle"))) {
      return null;
    }

    string fallback;
    try {
      fallback = Path.Combine(GenFilePaths.SaveDataFolderPath, "PickleFixtures");
    } catch (Exception) {
      fallback = Path.Combine(Directory.GetCurrentDirectory(), "pickle-fixtures");
    }

    Log.Warn(
        "pickle: {ModRoot} is not writable, so its recorded fixtures go to {Fallback}. " +
        "Copy one into the mod's Pickle/Fixtures/ and commit it when you are happy with it.",
        [modRoot, fallback]);

    return fallback;
  }

  /// <summary>Writes a probe file into the mod's Pickle/ rather than creating Fixtures/, so a
  /// mod that ships only features does not get an empty directory it never asked for.</summary>
  private static bool IsWritable(string dir) {
    if (!Directory.Exists(dir)) {
      return false;
    }

    try {
      string probe = Path.Combine(dir, $".pickle-write-probe-{Guid.NewGuid():N}");
      File.WriteAllText(probe, string.Empty);
      File.Delete(probe);
      return true;
    } catch {
      return false;
    }
  }
}
