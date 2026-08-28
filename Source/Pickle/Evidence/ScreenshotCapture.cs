using System;
using System.IO;
using System.Text.RegularExpressions;
using Pickle.Autorun;
using Pickle.Runtime;
using UnityEngine;
using Verse;

namespace Pickle.Evidence;

public static class ScreenshotCapture {
  private static string? resolvedDir;

  // Autorun resolves one report root and calls this, so screenshots land in the same
  // tree as junit.xml.
  public static void SetReportRoot(string reportsRoot) {
    resolvedDir = Path.Combine(reportsRoot, "screenshots");
    TryCreate(resolvedDir);
  }

  // Reuses ReportDirectoryResolver so screenshots land beside junit.xml. Interactive
  // runs never call SetReportRoot, so without this they fell back to cwd.
  public static string ReportsDirectory() {
    if (resolvedDir != null) {
      return resolvedDir;
    }

    string preferred = Path.Combine(ReportDirectoryResolver.Resolve(null), "screenshots");
    if (TryCreate(preferred)) {
      resolvedDir = preferred;
      return resolvedDir;
    }

    string fallback = Path.Combine(Path.GetTempPath(), "pickle-reports", "screenshots");
    Log.Warning($"pickle: cannot write evidence to {preferred}; using {fallback} instead");
    TryCreate(fallback);
    resolvedDir = fallback;
    return resolvedDir;
  }

  private static bool TryCreate(string dir) {
    try {
      if (!Directory.Exists(dir)) {
        Directory.CreateDirectory(dir);
      }

      return true;
    } catch {
      return false;
    }
  }

  public static string BuildScreenshotPath(string featureName, string scenarioName, int stepIndex) {
    string filename = $"{Sanitize(featureName)}--{Sanitize(scenarioName)}--step{stepIndex}.png";

    return Path.Combine(ReportsDirectory(), filename);
  }

  private static string Sanitize(string name) {
    return Regex.Replace(name, "[^A-Za-z0-9._-]", "-");
  }

  public static PickleWait CaptureToFile(string filePath) {
    return PickleDriver.Instance.CaptureScreenshot(filePath);
  }
}
