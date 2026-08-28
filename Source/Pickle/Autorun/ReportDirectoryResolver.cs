using System;
using System.IO;

namespace Pickle.Autorun;

/// <summary>
/// -pickle-report-dir wins. Otherwise prefer /out/pickle-reports, the writable mount in
/// the harness where cwd is read-only.
/// </summary>
public static class ReportDirectoryResolver {
  public static string Resolve(string? explicitDir) {
    if (!string.IsNullOrEmpty(explicitDir)) {
      return explicitDir!;
    }

    if (IsWritableDirectory("/out")) {
      return Path.Combine("/out", "pickle-reports");
    }

    return Path.Combine(Directory.GetCurrentDirectory(), "pickle-reports");
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
