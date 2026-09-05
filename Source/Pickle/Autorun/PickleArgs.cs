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
  public bool RunRequested { get; private set; }

  public string? RunFilter { get; private set; }

  public string? ReportDir { get; private set; }

  public bool IncludeWip { get; private set; }

  public int Seed { get; private set; } = RunSession.DefaultSeed;

  public int ScenarioTimeoutSeconds { get; private set; } = 120;

  /// <summary>Extra attempts a failed scenario gets. Zero runs each scenario once.</summary>
  public int Retries { get; private set; }

  /// <summary>Labels this run's reports, so a merged report can tell the mod sets apart.</summary>
  public string? SetName { get; private set; }

  /// <summary>How wait steps spend time. An unattended run is Fast unless -pickle-mode says otherwise.</summary>
  public PickleRunMode.Mode Mode { get; private set; } = PickleRunMode.Mode.Fast;

  public int RunTimeoutMinutes { get; private set; } = 60;

  /// <summary>Seconds of footage a filmed scenario keeps before it stops capturing.</summary>
  public int MaxFilmSeconds { get; private set; } = 60;

  public static PickleArgs Parse() {
    bool runBare = GenCommandLine.CommandLineArgPassed("-pickle-run");
    bool runValued = GenCommandLine.TryGetCommandLineArg("-pickle-run", out string runValue);

    string? cliFilter = runValued ? runValue : null;
    string? cliReportDir = GenCommandLine.TryGetCommandLineArg("-pickle-report-dir", out string reportDirValue)
        ? reportDirValue
        : null;
    bool cliIncludeWip = GenCommandLine.CommandLineArgPassed("-pickle-include-wip");
    int? cliSeed = GenCommandLine.TryGetCommandLineArg("-pickle-seed", out string seedValue)
        && int.TryParse(seedValue, out int parsedSeed)
            ? parsedSeed
            : null;
    int? cliScenarioTimeout = GenCommandLine.TryGetCommandLineArg("-pickle-scenario-timeout", out string scenarioTimeoutValue)
        && int.TryParse(scenarioTimeoutValue, out int parsedScenarioTimeout)
            ? parsedScenarioTimeout
            : null;

    string? cliSetName = GenCommandLine.TryGetCommandLineArg("-pickle-set-name", out string setNameValue)
        && !setNameValue.NullOrEmpty()
            ? setNameValue
            : null;

    int? cliRetries = GenCommandLine.TryGetCommandLineArg("-pickle-retry", out string retryValue)
        && int.TryParse(retryValue, out int parsedRetries)
            ? parsedRetries
            : null;

    int? cliMaxFilm = GenCommandLine.TryGetCommandLineArg("-pickle-max-film-seconds", out string maxFilmValue)
        && int.TryParse(maxFilmValue, out int parsedMaxFilm)
        ? parsedMaxFilm
        : null;
    int? cliRunTimeout = GenCommandLine.TryGetCommandLineArg("-pickle-run-timeout", out string runTimeoutValue)
        && int.TryParse(runTimeoutValue, out int parsedRunTimeout)
            ? parsedRunTimeout
            : null;

    PickleRunMode.Mode cliMode = PickleRunMode.Mode.Fast;
    if (GenCommandLine.TryGetCommandLineArg("-pickle-mode", out string modeValue)) {
      if (string.Equals(modeValue, "watch", StringComparison.OrdinalIgnoreCase)) {
        cliMode = PickleRunMode.Mode.Watch;
      } else if (!string.Equals(modeValue, "fast", StringComparison.OrdinalIgnoreCase)) {
        Log.Warn("pickle: -pickle-mode={Value} is not 'fast' or 'watch', running in fast", [modeValue]);
      }
    }

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

  private static PickleArgsConfig? LoadConfig(string path) {
    try {
      if (!File.Exists(path)) {
        Log.Warn("pickle: config file not found: {Path}", [path]);
        return null;
      }

      return PickleArgsConfig.Parse(File.ReadAllText(path));
    } catch (Exception ex) {
      Log.Error(ex, $"pickle: failed to read config {path}");
      return null;
    }
  }
}
