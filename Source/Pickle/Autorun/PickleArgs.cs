using System;
using System.IO;
using Pickle.Run;
using Verse;

namespace Pickle.Autorun;

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

  public int RunTimeoutMinutes { get; private set; } = 60;

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
    int? cliRunTimeout = GenCommandLine.TryGetCommandLineArg("-pickle-run-timeout", out string runTimeoutValue)
        && int.TryParse(runTimeoutValue, out int parsedRunTimeout)
            ? parsedRunTimeout
            : null;

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
      RunTimeoutMinutes = cliRunTimeout ?? config?.RunTimeoutMinutes ?? 60,
    };
  }

  private static PickleArgsConfig? LoadConfig(string path) {
    try {
      if (!File.Exists(path)) {
        Log.Warning($"pickle: config file not found: {path}");
        return null;
      }

      return PickleArgsConfig.Parse(File.ReadAllText(path));
    } catch (Exception ex) {
      Log.Error($"pickle: failed to read config {path}: {ex.Message}");
      return null;
    }
  }
}
