using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Runtime;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Entry point for -pickle-run. Waits for the main menu, runs the suite, then exits
/// with a code CI can read. Reports are rewritten whole after every scenario.
/// </summary>
[StaticConstructorOnStartup]
public static class AutorunBootstrap {
  private const int QuitGraceMs = 15000;

  static AutorunBootstrap() {
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
  }

  internal static void WriteReports(string reportDir, List<ScenarioResult> results, string exitReason, Action<string>? onError = null) {
    try {
      File.WriteAllText(Path.Combine(reportDir, "junit.xml"), JUnitReportWriter.Write(results));
      File.WriteAllText(
          Path.Combine(reportDir, "messages.ndjson"),
          MessagesNdjsonWriter.Write(results, path => File.Exists(path) ? File.ReadAllBytes(path) : null));
      File.WriteAllText(Path.Combine(reportDir, "summary.json"), SummaryJsonWriter.Write(results, exitReason));
      File.WriteAllText(Path.Combine(reportDir, "summary.md"), SummaryMarkdownWriter.Write(results));
      File.WriteAllText(
          Path.Combine(reportDir, "report.html"),
          HtmlReportWriter.Write(
              results,
              exitReason,
              Web.Dashboard.ReportTemplate,
              path => File.Exists(path) ? File.ReadAllBytes(path) : null,
              Web.DashboardStrings.BuildJson()));
    } catch (Exception ex) {
      string msg = $"pickle: failed writing reports: {ex.Message}";
      if (onError != null) {
        onError(msg);
      } else {
        Log.Error(msg);
      }
    }
  }

  private static async Task RunAutorun(PickleArgs args, string reportDir) {
    Log.Message($"pickle: autorun report dir = {reportDir}");
    Log.Message($"pickle: autorun seed = {args.Seed}");

    List<ScenarioResult> accumulated = new();
    int exitCode;

    try {
      AutorunState.IsAutorunning = true;
      PickleRunMode.Current = PickleRunMode.Mode.Fast;

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Entry, 180f);

      await SuiteRunner.Run(args.RunFilter, args.IncludeWip, args.Seed, scenario => {
        accumulated.Add(scenario);
        Watchdog.RecordProgress(accumulated);

        // TODO(perf): rewrites every file per scenario, O(n^2). Batch if a suite
        // ever runs to hundreds of scenarios.
        WriteReports(reportDir, accumulated, "in-progress");
      });

      exitCode = accumulated.Any(r => r.Outcome == ScenarioOutcome.Failed) ? 1 : 0;
    } catch (Exception ex) {
      Log.Error($"pickle: autorun infrastructure error: {ex}");
      exitCode = 2;
    } finally {
      AutorunState.IsAutorunning = false;
      Watchdog.Stop();
    }

    // After the run, not during it. Encoding is seconds of ffmpeg per scenario, and
    // doing it between scenarios would show up in every duration the report prints.
    EncodeFilms();

    WriteReports(reportDir, accumulated, exitCode switch {
      0 => "passed",
      1 => "failed",
      _ => "infrastructure-error",
    });

    Log.Message($"pickle: autorun exit code = {exitCode}");
    Quit(exitCode);
  }

  private static void EncodeFilms() {
    IReadOnlyList<(string Directory, double Fps)> films = FilmstripRecorder.RecordedFilms;
    if (films.Count == 0) {
      return;
    }

    if (!FilmEncoder.Available) {
      Log.Warning($"pickle: {films.Count} scenario(s) were filmed but ffmpeg is not on the PATH; frames were kept");
      return;
    }

    Log.Message($"pickle: encoding {films.Count} film(s) before exit");
    for (int i = 0; i < films.Count; i++) {
      (string dir, double fps) = films[i];
      Log.Message($"pickle: encoding film {i + 1}/{films.Count} at {fps:0.#} fps");
      FilmEncoder.TryEncode(dir, fps);
    }

    Log.Message($"pickle: encoded {films.Count} film(s)");
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
