using System;
using System.IO;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// -pickle-report-dir wins, then /out/pickle-reports for the harness mount. The save
/// folder is the last resort because the game directory is often read only.
/// </summary>
public static class ReportDirectoryResolver {
  public static string Resolve(string? explicitDir) {
    if (!string.IsNullOrEmpty(explicitDir)) {
      if (TryPrepare(explicitDir!)) {
        return explicitDir!;
      }

      // A bad path used to throw out of AutorunBootstrap's static constructor, which
      // killed the type and ran nothing. Name it and carry on instead.
      string fallback = ResolveDefault();
      Log.Error("pickle: report dir '{Dir}' is not writable, falling back to {Fallback}", [explicitDir!, fallback]);
      return fallback;
    }

    return ResolveDefault();
  }

  private static string ResolveDefault() {
    if (IsWritableDirectory("/out")) {
      return Path.Combine("/out", "pickle-reports");
    }

    try {
      return Path.Combine(GenFilePaths.SaveDataFolderPath, "PickleReports");
    } catch (Exception) {
      return Path.Combine(Directory.GetCurrentDirectory(), "pickle-reports");
    }
  }

  private static bool TryPrepare(string dir) {
    try {
      Directory.CreateDirectory(dir);
    } catch (Exception) {
      return false;
    }

    return IsWritableDirectory(dir);
  }

  private static bool IsWritableDirectory(string dir) {
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
