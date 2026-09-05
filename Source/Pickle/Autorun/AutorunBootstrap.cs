using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Runtime;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Entry point for -pickle-run. Waits for the main menu, runs the suite, then exits
/// with a code CI can read. Reports are rewritten whole after every scenario.
/// </summary>
[StaticConstructorOnStartup]
public static class AutorunBootstrap {
  private const int QuitGraceMs = 15000;

  // Nothing here may escape: anything thrown out of a static constructor becomes a
  // TypeInitializationException that kills the type, so the run silently never starts.
  static AutorunBootstrap() {
    try {
      PickleArgs args = PickleArgs.Parse();
      if (!args.RunRequested) {
        return;
      }

      // Resolved here because a static constructor is guaranteed main thread.
      // RunAutorun starts on a background thread, where consoleLogPath is unsafe.
      string reportDir = ReportDirectoryResolver.Resolve(args.ReportDir);
      Directory.CreateDirectory(reportDir);
      ScreenshotCapture.SetReportRoot(reportDir);
      FilmstripRecorder.MaxSeconds = args.MaxFilmSeconds;

      Watchdog.Start(args.ScenarioTimeoutSeconds, args.RunTimeoutMinutes, reportDir, Application.consoleLogPath);

      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunAutorun(args, reportDir), "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
    } catch (Exception ex) {
      Log.Error(ex, "pickle: autorun failed to start");
    }
  }

  internal static void WriteReports(
      string reportDir, List<ScenarioResult> results, string exitReason,
      Action<string>? onError = null, string? setName = null) {
    try {
      File.WriteAllText(Path.Combine(reportDir, "junit.xml"), JUnitReportWriter.Write(results));
      File.WriteAllText(
          Path.Combine(reportDir, "messages.ndjson"),
          MessagesNdjsonWriter.Write(results, path => File.Exists(path) ? File.ReadAllBytes(path) : null));
      File.WriteAllText(Path.Combine(reportDir, "summary.json"), SummaryJsonWriter.Write(results, exitReason, setName));
      File.WriteAllText(Path.Combine(reportDir, "summary.md"), SummaryMarkdownWriter.Write(results));
      File.WriteAllText(
          Path.Combine(reportDir, "report.html"),
          HtmlReportWriter.Write(
              results,
              exitReason,
              Web.Dashboard.ReportTemplate,
              path => File.Exists(path) ? File.ReadAllBytes(path) : null,
              Web.DashboardStrings.BuildJson(),
              setName));
    } catch (Exception ex) {
      string msg = $"pickle: failed writing reports: {ex.Message}";
      if (onError != null) {
        onError(msg);
      } else {
        Log.Error("{Message}", [msg]);
      }
    }
  }

  private static async Task RunAutorun(PickleArgs args, string reportDir) {
    Log.Info("pickle: autorun report dir = {ReportDir}", [reportDir]);
    Log.Info("pickle: autorun seed = {Seed}", [args.Seed]);
    Log.Info("pickle: autorun retries = {Retries}", [args.Retries]);

    List<ScenarioResult> accumulated = new();
    int exitCode;

    try {
      AutorunState.IsAutorunning = true;

      // Set here, not in the static constructor: RunSession restores the mode it found
      // after every scenario, so the value has to be in place when the run starts.
      PickleRunMode.Current = args.Mode;

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Entry, 180f);

      await SuiteRunner.Run(args.RunFilter, args.Seed, retries: args.Retries, onScenarioCompleted: scenario => {
        accumulated.Add(scenario);
        Watchdog.RecordProgress(accumulated);

        // TODO(perf): rewrites every file per scenario, O(n^2). Batch if a suite
        // ever runs to hundreds of scenarios.
        WriteReports(reportDir, accumulated, "in-progress", setName: args.SetName);
      });

      exitCode = accumulated.Any(r => r.Outcome == ScenarioOutcome.Failed) ? 1 : 0;
    } catch (Exception ex) {
      Log.Error(ex, "pickle: autorun infrastructure error");
      exitCode = 2;
    } finally {
      AutorunState.IsAutorunning = false;
      Watchdog.Stop();
    }

    // After the run, not during it. Encoding is seconds of ffmpeg per scenario, and
    // doing it between scenarios would show up in every duration the report prints.
    EncodeFilms();

    WriteReports(
        reportDir,
        accumulated,
        exitCode switch {
          0 => "passed",
          1 => "failed",
          _ => "infrastructure-error",
        },
        setName: args.SetName);

    Log.Info("pickle: autorun exit code = {ExitCode}", [exitCode]);
    Quit(exitCode);
  }

  private static void EncodeFilms() {
    IReadOnlyList<(string Directory, double Fps)> films = FilmstripRecorder.RecordedFilms;
    if (films.Count == 0) {
      return;
    }

    if (!FilmEncoder.Available) {
      Log.Warn(
          "pickle: {Count} scenario(s) were filmed but ffmpeg is not on the PATH; frames were kept",
          [films.Count]);
      return;
    }

    Log.Info("pickle: encoding {Count} film(s) before exit", [films.Count]);
    for (int i = 0; i < films.Count; i++) {
      (string dir, double fps) = films[i];
      Log.Info(
          "pickle: encoding film {Index}/{Count} at {Fps} fps",
          [i + 1, films.Count, fps.ToString("0.#", CultureInfo.InvariantCulture)]);
      FilmEncoder.TryEncode(dir, fps);
    }

    Log.Info("pickle: encoded {Count} film(s)", [films.Count]);
  }

  // Environment.Exit does not end the process under Unity's Mono, so a passing run
  // hung until the harness timeout. Unity's own quit ends it and carries the code.
  private static void Quit(int exitCode) {
    System.Threading.Thread killer = new System.Threading.Thread(() => {
      System.Threading.Thread.Sleep(QuitGraceMs);
      Console.Error.WriteLine("pickle: Application.Quit did not end the process, force-killing");
      System.Diagnostics.Process.GetCurrentProcess().Kill();
    }) { IsBackground = true };

    killer.Start();
    Application.Quit(exitCode);
  }
}
