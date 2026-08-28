using System.Text.RegularExpressions;

namespace Pickle.Autorun;

/// <summary>
/// Fields mirror the CLI flags one to one. Hand-rolled extraction because the object is
/// small, flat, and known ahead of time.
/// </summary>
public sealed class PickleArgsConfig {
  public string? Filter { get; private set; }

  public string? ReportDir { get; private set; }

  public bool? IncludeWip { get; private set; }

  public int? Seed { get; private set; }

  public int? ScenarioTimeoutSeconds { get; private set; }

  public int? RunTimeoutMinutes { get; private set; }

  public static PickleArgsConfig Parse(string json) {
    return new PickleArgsConfig {
      Filter = ExtractString(json, "run"),
      ReportDir = ExtractString(json, "reportDir"),
      IncludeWip = ExtractBool(json, "includeWip"),
      Seed = ExtractInt(json, "seed"),
      ScenarioTimeoutSeconds = ExtractInt(json, "scenarioTimeout"),
      RunTimeoutMinutes = ExtractInt(json, "runTimeout"),
    };
  }

  private static string? ExtractString(string json, string key) {
    Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*\"([^\"]*)\"");
    return match.Success ? match.Groups[1].Value : null;
  }

  private static bool? ExtractBool(string json, string key) {
    Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(true|false)");
    return match.Success ? bool.Parse(match.Groups[1].Value) : null;
  }

  private static int? ExtractInt(string json, string key) {
    Match match = Regex.Match(json, $"\"{key}\"\\s*:\\s*(-?\\d+)");
    return match.Success ? int.Parse(match.Groups[1].Value) : null;
  }
}
