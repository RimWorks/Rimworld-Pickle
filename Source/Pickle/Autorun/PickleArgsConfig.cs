using System;
using System.Text.RegularExpressions;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Fields mirror the CLI flags one to one. Hand-rolled extraction because the object is
/// small, flat, and known ahead of time.
/// </summary>
public sealed class PickleArgsConfig {
  private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

  /// <summary>The <c>run</c> filter, or <c>null</c> if the config left it unset.</summary>
  public string? Filter { get; private set; }

  /// <summary>The <c>reportDir</c> path, or <c>null</c> if the config left it unset.</summary>
  public string? ReportDir { get; private set; }

  /// <summary>Whether <c>@wip</c> scenarios should run, or <c>null</c> if the config left it unset.</summary>
  public bool? IncludeWip { get; private set; }

  /// <summary>The random seed to run with, or <c>null</c> if the config left it unset.</summary>
  public int? Seed { get; private set; }

  /// <summary>Per-scenario timeout in seconds, or <c>null</c> if the config left it unset.</summary>
  public int? ScenarioTimeoutSeconds { get; private set; }

  /// <summary>Whole-run timeout in minutes, or <c>null</c> if the config left it unset.</summary>
  public int? RunTimeoutMinutes { get; private set; }

  /// <summary>Extra attempts a failed scenario gets, or <c>null</c> if the config left it unset.</summary>
  public int? Retries { get; private set; }

  /// <summary>The run's set name, or <c>null</c> if the config left it unset.</summary>
  public string? SetName { get; private set; }

  /// <summary>Reads the fields the CLI's <c>-pickle-config</c> flag understands out of a JSON blob.</summary>
  /// <param name="json">The config file's raw text.</param>
  /// <returns>A config with every field the JSON set, and <c>null</c> for the rest.</returns>
  public static PickleArgsConfig Parse(string json) {
    return new PickleArgsConfig {
      Filter = ExtractString(json, "run"),
      ReportDir = ExtractString(json, "reportDir"),
      IncludeWip = ExtractBool(json, "includeWip"),
      Seed = ExtractInt(json, "seed"),
      ScenarioTimeoutSeconds = ExtractInt(json, "scenarioTimeout"),
      RunTimeoutMinutes = ExtractInt(json, "runTimeout"),
      Retries = ExtractInt(json, "retry"),
      SetName = ExtractString(json, "setName"),
    };
  }

  private static string? ExtractString(string json, string key) {
    Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"([^\"]*)\"", RegexOptions.None, RegexTimeout);
    return match.Success ? match.Groups[1].Value : null;
  }

  private static bool? ExtractBool(string json, string key) {
    Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(true|false)", RegexOptions.None, RegexTimeout);
    return match.Success ? bool.Parse(match.Groups[1].Value) : null;
  }

  private static int? ExtractInt(string json, string key) {
    Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(-?\\d+)", RegexOptions.None, RegexTimeout);
    return match.Success ? int.Parse(match.Groups[1].Value) : null;
  }
}
