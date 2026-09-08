using System;
using System.IO;
using RimWorks.Pickle.Run;
using RimWorks.Pickle.Runtime;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Parses the -pickle-* command line flags. An explicit flag wins over the same field
/// in -pickle-config, which only fills what the CLI did not set.
/// </summary>
public sealed class PickleArgs {
  /// <summary>Whether <c>-pickle-run</c> was passed at all, with or without a filter.</summary>
  public bool RunRequested { get; private set; }

  /// <summary>The scenario filter from <c>-pickle-run</c>'s value, or <c>null</c> to run everything.</summary>
  public string? RunFilter { get; private set; }

  /// <summary>Where reports and screenshots land. <c>null</c> falls back to <see cref="ReportDirectoryResolver"/>.</summary>
  public string? ReportDir { get; private set; }

  /// <summary>Whether <c>@wip</c> scenarios run alongside everything else.</summary>
  public bool IncludeWip { get; private set; }

  /// <summary>The random seed the run uses, so a failure can be reproduced.</summary>
  public int Seed { get; private set; } = RunSession.DefaultSeed;

  /// <summary>Seconds a single scenario gets before it is treated as timed out.</summary>
  public int ScenarioTimeoutSeconds { get; private set; } = 120;

  /// <summary>Extra attempts a failed scenario gets. Zero runs each scenario once.</summary>
  public int Retries { get; private set; }

  /// <summary>Labels this run's reports, so a merged report can tell the mod sets apart.</summary>
  public string? SetName { get; private set; }

  /// <summary>How wait steps spend time. An unattended run is Fast unless -pickle-mode says otherwise.</summary>
  public PickleRunMode.Mode Mode { get; private set; } = PickleRunMode.Mode.Fast;

  /// <summary>Minutes the whole run gets before <see cref="Watchdog"/> kills it.</summary>
  public int RunTimeoutMinutes { get; private set; } = 60;

  /// <summary>Seconds of footage a filmed scenario keeps before it stops capturing.</summary>
  public int MaxFilmSeconds { get; private set; } = 60;

  /// <summary>Reads the command line, then fills anything unset from <c>-pickle-config</c>.</summary>
  /// <returns>The resolved args for this run.</returns>
  public static PickleArgs Parse() {
    bool runBare = GenCommandLine.CommandLineArgPassed("-pickle-run");
    bool runValued = GenCommandLine.TryGetCommandLineArg("-pickle-run", out string runValue);

    string? cliFilter = runValued ? runValue : null;
    string? cliReportDir = StringArg("-pickle-report-dir");
    bool cliIncludeWip = GenCommandLine.CommandLineArgPassed("-pickle-include-wip");
    int? cliSeed = IntArg("-pickle-seed");
    int? cliScenarioTimeout = IntArg("-pickle-scenario-timeout");
    string? cliSetName = NonEmptyArg("-pickle-set-name");
    int? cliRetries = IntArg("-pickle-retry");
    int? cliMaxFilm = IntArg("-pickle-max-film-seconds");
    int? cliRunTimeout = IntArg("-pickle-run-timeout");
    PickleRunMode.Mode cliMode = ModeArg();

    PickleArgsConfig? config = null;
    if (GenCommandLine.TryGetCommandLineArg("-pickle-config", out string configPath)) {
      config = LoadConfig(configPath);
    }

    return new PickleArgs {
      RunRequested = runBare || runValued,
      RunFilter = cliFilter ?? config?.Filter,
      ReportDir = cliReportDir ?? config?.ReportDir,
      IncludeWip = cliIncludeWip || (config?.IncludeWip ?? false),
      Seed = cliSeed ?? config?.Seed ?? RunSession.DefaultSeed,
      ScenarioTimeoutSeconds = cliScenarioTimeout ?? config?.ScenarioTimeoutSeconds ?? 120,
      Retries = Math.Max(0, cliRetries ?? config?.Retries ?? 0),
      SetName = cliSetName ?? config?.SetName,
      RunTimeoutMinutes = cliRunTimeout ?? config?.RunTimeoutMinutes ?? 60,
      MaxFilmSeconds = cliMaxFilm ?? 60,
      Mode = cliMode,
    };
  }

  private static string? StringArg(string name) {
    return GenCommandLine.TryGetCommandLineArg(name, out string value) ? value : null;
  }

  private static string? NonEmptyArg(string name) {
    return GenCommandLine.TryGetCommandLineArg(name, out string value) && !value.NullOrEmpty() ? value : null;
  }

  private static int? IntArg(string name) {
    return GenCommandLine.TryGetCommandLineArg(name, out string value) && int.TryParse(value, out int parsed) ? parsed : null;
  }

  private static PickleRunMode.Mode ModeArg() {
    if (!GenCommandLine.TryGetCommandLineArg("-pickle-mode", out string value)) {
      return PickleRunMode.Mode.Fast;
    }

    if (string.Equals(value, "watch", StringComparison.OrdinalIgnoreCase)) {
      return PickleRunMode.Mode.Watch;
    }

    if (!string.Equals(value, "fast", StringComparison.OrdinalIgnoreCase)) {
      Log.WarnTo(PickleLog.Channel, "-pickle-mode={Value} is not 'fast' or 'watch', running in fast", [value]);
    }

    return PickleRunMode.Mode.Fast;
  }

  private static PickleArgsConfig? LoadConfig(string path) {
    try {
      if (!File.Exists(path)) {
        Log.WarnTo(PickleLog.Channel, "config file not found: {Path}", [path]);
        return null;
      }

      return PickleArgsConfig.Parse(File.ReadAllText(path));
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, $"failed to read config {path}");
      return null;
    }
  }
}
