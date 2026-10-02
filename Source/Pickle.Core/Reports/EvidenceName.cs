using System;
using System.Globalization;
using System.Text.RegularExpressions;

namespace RimWorks.Pickle.Core.Reports;

/// <summary>Builds the file-safe stem that a screenshot or film folder is named after.</summary>
public static class EvidenceName {
  /// <summary>How many characters of the full name survive when it has to be truncated.</summary>
  public const int MaxStemLength = 60;

  private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

  /// <summary>Builds the stem for one scenario's evidence, short enough to survive a 260 character path.</summary>
  /// <param name="featureName">The feature the scenario belongs to.</param>
  /// <param name="scenarioName">The scenario the evidence was captured for.</param>
  /// <returns>The sanitized name when it fits, otherwise its first <see cref="MaxStemLength"/> characters
  /// followed by a hyphen and eight hex characters of a hash of the whole name.</returns>
  public static string Stem(string? featureName, string? scenarioName) {
    string full = Sanitize(featureName) + "--" + Sanitize(scenarioName);
    if (full.Length <= MaxStemLength) {
      return full;
    }

    return full.Remove(MaxStemLength) + "-" + ProcessStableShortHash(full);
  }

  private static string Sanitize(string? name) {
    return Regex.Replace(name ?? string.Empty, "[^A-Za-z0-9._-]", "-", RegexOptions.None, RegexTimeout);
  }

  private static string ProcessStableShortHash(string value) {
    uint hash = 2166136261;
    foreach (char c in value) {
      hash = unchecked((hash ^ c) * 16777619);
    }

    return hash.ToString("x8", CultureInfo.InvariantCulture);
  }
}
