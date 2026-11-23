using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Hosting;
using RimWorks.Pickle.Runtime;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Entry point for -pickle-run. Waits for the main menu, runs the suite, then exits
/// with a code CI can read. Reports are rewritten whole after every scenario.
/// </summary>
public static class AutorunBootstrap {
  private const double InProgressWriteSeconds = 5;

  /// <summary>Prepares a <c>-pickle-run</c> on the main thread once the game has loaded its content.
  /// The host runs the returned task in the background, and it quits the game when the run ends.</summary>
  /// <returns>The run to start, or <c>null</c> when no run was requested or it failed to start.</returns>
  public static Func<Task>? Start() {
    try {
      PickleArgs args = PickleArgs.Parse();
      if (!args.RunRequested) {
        return null;
      }

      string reportDir = ReportDirectoryResolver.Resolve(args.ReportDir);
      Directory.CreateDirectory(reportDir);
      ScreenshotCapture.SetReportRoot(reportDir);
      FilmstripRecorder.MaxSeconds = args.MaxFilmSeconds;

      string? consoleLogPath = HostRegistry.TryGet(out IGameApp? app) ? app!.ConsoleLogPath : null;
      Watchdog.Start(args.ScenarioTimeoutSeconds, args.RunTimeoutMinutes, reportDir, consoleLogPath);

      PickleDriver.EnsureExists();
      return () => RunAutorun(args, reportDir);
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "autorun failed to start");
      return null;
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

      // last: a reader that sees a fresh summary.json can trust every other file beside it
      File.WriteAllText(Path.Combine(reportDir, "summary.json"), SummaryJsonWriter.Write(results, exitReason, setName));
    } catch (Exception ex) {
      if (onError != null) {
        onError($"pickle: failed writing reports: {ex.Message}");
      } else {
        Log.ErrorTo(PickleLog.Channel, "failed writing reports: {Message}", [ex.Message]);
      }
    }
  }

  private static async Task RunAutorun(PickleArgs args, string reportDir) {
    Log.InfoTo(PickleLog.Channel, "autorun report dir = {ReportDir}", [reportDir]);
    Log.InfoTo(PickleLog.Channel, "autorun seed = {Seed}", [args.Seed]);
    Log.InfoTo(PickleLog.Channel, "autorun retries = {Retries}", [args.Retries]);

    List<ScenarioResult> accumulated = new();
    int exitCode;

    try {
      AutorunState.IsAutorunning = true;

      // Set here, not in the static constructor: RunSession restores the mode it found
      // after every scenario, so the value has to be in place when the run starts.
      PickleRunMode.Current = args.Mode;

      PickleDriver driver = PickleDriver.Instance;
      if (HostRegistry.TryGet(out IGameState? state)) {
        await driver.WaitUntil(() => state!.IsMainMenuUp, 180f);
      }

      // Every write rewrites all of accumulated, so writing per scenario is O(n^2) over a
      // suite. The in-progress report only has to be fresh enough for someone watching, so
      // it goes out on an interval. The first one is unconditional, so the files exist early.
      Stopwatch sinceWrite = Stopwatch.StartNew();

      await SuiteRunner.Run(args.RunFilter, args.Seed, retries: args.Retries, onScenarioCompleted: scenario => {
        accumulated.Add(scenario);
        Watchdog.RecordProgress(accumulated);

        if (accumulated.Count > 1 && sinceWrite.Elapsed.TotalSeconds < InProgressWriteSeconds) {
          return;
        }

        sinceWrite.Restart();
        WriteReports(reportDir, accumulated, "in-progress", setName: args.SetName);
      });

      exitCode = accumulated.Any(r => r.Outcome == ScenarioOutcome.Failed) ? 1 : 0;
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "autorun infrastructure error");
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

    // An error can land after its scenario ends, most often from an async callback. It is
    // recorded but blamed on nobody, so the count is the only place it shows up.
    if (LogWatch.OutsideScenarioCount > 0) {
      Log.WarnTo(PickleLog.Channel,
          "{Count} error(s) were logged outside any scenario and failed nothing",
          [LogWatch.OutsideScenarioCount]);
    }

    Log.InfoTo(PickleLog.Channel, "autorun exit code = {ExitCode}", [exitCode]);
    if (HostRegistry.TryGet(out IGameApp? gameApp)) {
      gameApp!.Quit(exitCode);
    } else {
      Environment.Exit(exitCode);
    }
  }

  private static void EncodeFilms() {
    IReadOnlyList<(string Directory, double Fps)> films = FilmstripRecorder.RecordedFilms;
    if (films.Count == 0) {
      return;
    }

    if (!FilmEncoder.Available) {
      Log.WarnTo(PickleLog.Channel,
          "{Count} scenario(s) were filmed but ffmpeg is not on the PATH; frames were kept",
          [films.Count]);
      return;
    }

    Log.InfoTo(PickleLog.Channel, "encoding {Count} film(s) before exit", [films.Count]);
    for (int i = 0; i < films.Count; i++) {
      (string dir, double fps) = films[i];
      Log.InfoTo(PickleLog.Channel,
          "encoding film {Index}/{Count} at {Fps} fps",
          [i + 1, films.Count, fps.ToString("0.#", CultureInfo.InvariantCulture)]);
      FilmEncoder.TryEncode(dir, fps);
    }

    Log.InfoTo(PickleLog.Channel, "encoded {Count} film(s)", [films.Count]);
  }
}
