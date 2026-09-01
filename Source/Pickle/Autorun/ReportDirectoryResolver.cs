using System;
using System.IO;
using Verse;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// -pickle-report-dir wins, then /out/pickle-reports for the harness mount. The save
/// folder is the last resort because the game directory is often read only.
/// </summary>
public static class ReportDirectoryResolver {
  public static string Resolve(string? explicitDir) {
    if (!string.IsNullOrEmpty(explicitDir)) {
      return explicitDir!;
    }

    if (IsWritableDirectory("/out")) {
      return Path.Combine("/out", "pickle-reports");
    }

    try {
      return Path.Combine(GenFilePaths.SaveDataFolderPath, "PickleReports");
    } catch (Exception) {
      return Path.Combine(Directory.GetCurrentDirectory(), "pickle-reports");
    }
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
