using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Threading;
using Pickle.Core.Run;

namespace Pickle.Autorun;

/// <summary>
/// Polls on a ThreadPool timer, so it still fires when the main thread wedges and
/// Update stops. Never calls Verse.Log, which needs that thread and shares its lock.
/// </summary>
public static class Watchdog {
  private static readonly object StateLock = new();

  private static Timer? timer;
  private static string? logPath;
  private static string? reportDir;
  private static int scenarioTimeoutSeconds;
  private static int runTimeoutMinutes;
  private static DateTime runDeadlineUtc;
  private static int tripped;

  private static string? currentFeature;
  private static string? currentScenario;
  private static DateTime scenarioStartUtc;
  private static string? lastStep;
  private static DateTime lastActivityUtc;
  private static List<ScenarioResult> lastProgress = new();

  // Catches hangs between scenarios, where the in-scenario branch sees nothing in
  // flight. Floored above the scenario timeout so a slow fixture load cannot trip it.
  private static int IdleTimeoutSeconds => Math.Max(scenarioTimeoutSeconds, 60);

  public static void Start(int scenarioTimeoutSeconds, int runTimeoutMinutes, string reportDir, string logPath) {
    Watchdog.scenarioTimeoutSeconds = scenarioTimeoutSeconds;
    Watchdog.runTimeoutMinutes = runTimeoutMinutes;
    Watchdog.reportDir = reportDir;
    Watchdog.logPath = logPath;
    runDeadlineUtc = DateTime.UtcNow.AddMinutes(runTimeoutMinutes);
    tripped = 0;
    currentFeature = null;
    currentScenario = null;
    lastStep = null;
    lastActivityUtc = DateTime.UtcNow;
    lastProgress = new List<ScenarioResult>();

    timer = new Timer(_ => CheckTimeouts(), null, 1000, 1000);
  }

  public static void Stop() {
    timer?.Dispose();
    timer = null;
  }

  public static void BeginScenario(string featureName, string scenarioName) {
    lock (StateLock) {
      currentFeature = featureName;
      currentScenario = scenarioName;
      scenarioStartUtc = DateTime.UtcNow;
      lastStep = null;
      lastActivityUtc = DateTime.UtcNow;
    }
  }

  public static void EndScenario() {
    lock (StateLock) {
      currentFeature = null;
      currentScenario = null;
      lastStep = null;
      lastActivityUtc = DateTime.UtcNow;
    }
  }

  public static void Heartbeat(string what) {
    lock (StateLock) {
      lastStep = what;
      lastActivityUtc = DateTime.UtcNow;
    }
  }

  public static void RecordProgress(List<ScenarioResult> results) {
    lock (StateLock) {
      lastProgress = [.. results];
      lastActivityUtc = DateTime.UtcNow;
    }
  }

  private static void CheckTimeouts() {
    if (tripped != 0) {
      return;
    }

    string? feature;
    string? scenario;
    string? step;
    DateTime scenarioStart;
    DateTime lastActivity;
    ScenarioResult? lastFinished;
    lock (StateLock) {
      feature = currentFeature;
      scenario = currentScenario;
      step = lastStep;
      scenarioStart = scenarioStartUtc;
      lastActivity = lastActivityUtc;
      lastFinished = lastProgress.Count > 0 ? lastProgress[lastProgress.Count - 1] : null;
    }

    DateTime now = DateTime.UtcNow;

    if (now >= runDeadlineUtc) {
      string where = scenario != null
          ? $"currently in scenario '{scenario}' (feature '{feature}')"
          : "suite discovery/setup still in progress";
      string stepPart = step != null ? $", last step: '{step}'" : string.Empty;
      Trip($"pickle: watchdog tripped after {runTimeoutMinutes}m run timeout, {where}{stepPart}");
      return;
    }

    if (scenario != null && (now - scenarioStart).TotalSeconds >= scenarioTimeoutSeconds) {
      string stepPart = step != null ? $", last step: '{step}'" : ", no step started yet";
      Trip($"pickle: watchdog tripped after {scenarioTimeoutSeconds}s in scenario '{scenario}' (feature '{feature}'){stepPart}");
      return;
    }

    int idleTimeoutSeconds = IdleTimeoutSeconds;
    if ((now - lastActivity).TotalSeconds >= idleTimeoutSeconds) {
      string where = lastFinished != null
          ? $"last scenario '{lastFinished.ScenarioName}' (feature '{lastFinished.FeatureName}') finished, next not started"
          : "suite discovery/setup, no scenario started yet";
      Trip($"pickle: watchdog tripped after {idleTimeoutSeconds}s idle - {where}");
    }
  }

  private static void Trip(string message) {
    if (Interlocked.CompareExchange(ref tripped, 1, 0) != 0) {
      return;
    }

    WriteLineSafe(message);

    List<ScenarioResult> results;
    lock (StateLock) {
      results = lastProgress;
    }

    if (reportDir != null) {
      AutorunBootstrap.WriteReports(reportDir, results, "watchdog-timeout", WriteLineSafe);
    }

    WriteLineSafe("pickle: watchdog forcing exit, code 2");

    // Environment.Exit neither ends the process under Unity's Mono nor returns, so it
    // runs on its own thread and the kill stays here. Death is by signal.
    Thread exitAttempt = new Thread(() => Environment.Exit(2)) { IsBackground = true };
    exitAttempt.Start();

    Thread.Sleep(3000);
    WriteLineSafe("pickle: watchdog Environment.Exit did not end the process, force-killing");
    Process.GetCurrentProcess().Kill();
  }

  private static void WriteLineSafe(string message) {
    try {
      if (logPath != null) {
        File.AppendAllText(logPath, message + Environment.NewLine);
      }
    } catch {
      // Nothing else to fall back to from a background thread mid-trip.
    }
  }
}
