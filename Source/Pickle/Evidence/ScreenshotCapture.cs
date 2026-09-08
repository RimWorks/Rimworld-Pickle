using System;
using System.IO;
using System.Text.RegularExpressions;
using RimWorks.Pickle.Autorun;
using RimWorks.Pickle.Runtime;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Evidence;

/// <summary>Resolves where screenshots and film frames land, and builds their file paths.</summary>
public static class ScreenshotCapture {
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

  private static string? resolvedDir;

  /// <summary>Points the screenshots folder at a report root chosen by autorun.</summary>
  /// <param name="reportsRoot">The report root to nest a screenshots folder under.</param>
  // Autorun resolves one report root and calls this, so screenshots land in the same
  // tree as junit.xml.
  public static void SetReportRoot(string reportsRoot) {
    resolvedDir = Path.Combine(reportsRoot, "screenshots");
    TryCreate(resolvedDir);
  }

  /// <summary>The screenshots folder, resolving and caching it on first use.</summary>
  /// <returns>The screenshots directory, created if it did not already exist.</returns>
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
    Log.WarnTo(PickleLog.Channel,
        "cannot write evidence to {Preferred}; using {Fallback} instead",
        [preferred, fallback]);
    TryCreate(fallback);
    resolvedDir = fallback;
    return resolvedDir;
  }

  /// <summary>The directory the screenshots folder sits in, which is where reports go.</summary>
  /// <returns>The report root directory.</returns>
  public static string ReportRoot() {
    return Path.GetDirectoryName(ReportsDirectory()) ?? ReportsDirectory();
  }

  /// <summary>Builds the path a screenshot for one step should be written to.</summary>
  /// <param name="featureName">The feature the step belongs to.</param>
  /// <param name="scenarioName">The scenario the step belongs to.</param>
  /// <param name="stepIndex">The step's position in the scenario.</param>
  /// <returns>The full path to write the screenshot to.</returns>
  public static string BuildScreenshotPath(string featureName, string scenarioName, int stepIndex) {
    string filename = $"{Sanitize(featureName)}--{Sanitize(scenarioName)}--step{stepIndex}.png";

    return Path.Combine(ReportsDirectory(), filename);
  }

  /// <summary>The folder a scenario's filmstrip frames are written into, creating it if needed.</summary>
  /// <param name="featureName">The feature the scenario belongs to.</param>
  /// <param name="scenarioName">The scenario being filmed.</param>
  /// <returns>The frame folder for the scenario.</returns>
  // One folder per scenario with plain numbered names, because ffmpeg reads a sequence
  // by pattern and cannot see a scenario name embedded in the file name.
  public static string FrameDirectory(string featureName, string scenarioName) {
    string dir = Path.Combine(ReportsDirectory(), "film", $"{Sanitize(featureName)}--{Sanitize(scenarioName)}");
    TryCreate(dir);

    return dir;
  }

  /// <summary>Builds the path one filmstrip frame should be written to.</summary>
  /// <param name="featureName">The feature the scenario belongs to.</param>
  /// <param name="scenarioName">The scenario being filmed.</param>
  /// <param name="frameIndex">The frame's position in the sequence.</param>
  /// <returns>The full path to write the frame to.</returns>
  public static string BuildFramePath(string featureName, string scenarioName, int frameIndex) {
    return Path.Combine(FrameDirectory(featureName, scenarioName), $"{frameIndex:D4}.jpg");
  }

  /// <summary>Starts an async screenshot capture to a file.</summary>
  /// <param name="filePath">The path to write the screenshot to.</param>
  /// <returns>A wait that completes once the file is written.</returns>
  public static PickleWait CaptureToFile(string filePath) {
    return PickleDriver.Instance.CaptureScreenshot(filePath);
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

  private static string Sanitize(string name) {
    return Regex.Replace(name, "[^A-Za-z0-9._-]", "-", RegexOptions.None, RegexTimeout);
  }
}
