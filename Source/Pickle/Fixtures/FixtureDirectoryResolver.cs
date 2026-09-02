using System;
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

  private static bool resolvedOnce;
  private static string? resolved;

  /// <summary>
  /// Null when the mod folders are writable, which leaves the committed layout alone.
  /// </summary>
  /// <param name="probeRoot">A mod root to test for writability. Null skips the test.</param>
  /// <returns>The writable root, or null to write into each mod's own Pickle/Fixtures/.</returns>
  public static string? Resolve(string? probeRoot) {
    if (resolvedOnce) {
      return resolved;
    }

    resolvedOnce = true;

    if (GenCommandLine.TryGetCommandLineArg(ArgName, out string explicitDir) && !explicitDir.NullOrEmpty()) {
      resolved = explicitDir;
      return resolved;
    }

    if (probeRoot != null && IsWritable(probeRoot)) {
      resolved = null;
      return null;
    }

    try {
      resolved = Path.Combine(GenFilePaths.SaveDataFolderPath, "PickleFixtures");
    } catch (Exception) {
      resolved = Path.Combine(Directory.GetCurrentDirectory(), "pickle-fixtures");
    }

    Log.Warn(
        "pickle: {ModRoot} is not writable, so recorded fixtures go to {Resolved}. " +
        "Copy one into the mod's Pickle/Fixtures/ and commit it when you are happy with it.",
        [probeRoot ?? "(no mod root)", resolved!]);

    return resolved;
  }

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
