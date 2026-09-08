using System;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>
/// The part of a save you can read without loading it. Everything here sits in the first
/// couple of KB of the file; the colony and its pawns are megabytes further in.
/// </summary>
public class FixtureHeader {
  private const int HeadChars = 64 * 1024;

  private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

  private static readonly Regex GameVersionPattern =
      new Regex("<gameVersion>([^<]*)</gameVersion>", RegexOptions.None, RegexTimeout);

  private static readonly Regex ScenarioNamePattern =
      new Regex("<scenario>\\s*<name>([^<]*)</name>", RegexOptions.None, RegexTimeout);

  private static readonly Regex ModIdsPattern =
      new Regex("<modIds>(.*?)</modIds>", RegexOptions.Singleline, RegexTimeout);

  private static readonly Regex ListItemPattern = new Regex("<li>", RegexOptions.None, RegexTimeout);

  /// <summary>Initializes a new instance.</summary>
  /// <param name="gameVersion">The version the save was written with, or <c>null</c> if unreadable.</param>
  /// <param name="scenarioName">The scenario's name, or <c>null</c> if unreadable.</param>
  /// <param name="modCount">How many mods the save lists as loaded.</param>
  public FixtureHeader(string? gameVersion, string? scenarioName, int modCount) {
    GameVersion = gameVersion;
    ScenarioName = scenarioName;
    ModCount = modCount;
  }

  /// <summary>The version the save was written with, or <c>null</c> when it could not be read.</summary>
  public string? GameVersion { get; }

  /// <summary>The scenario's name, or <c>null</c> when it could not be read.</summary>
  public string? ScenarioName { get; }

  /// <summary>How many mods the save lists as loaded.</summary>
  public int ModCount { get; }

  /// <summary>An unreadable or non-save file reads as an empty header rather than throwing.</summary>
  /// <param name="path">The save file to read the header from.</param>
  /// <returns>The header, empty when the file could not be read.</returns>
  public static FixtureHeader Read(string path) {
    string head;
    try {
      using StreamReader reader = new StreamReader(path, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
      char[] buffer = new char[HeadChars];
      int read = reader.ReadBlock(buffer, 0, buffer.Length);
      head = new string(buffer, 0, read);
    } catch (Exception) {
      return new FixtureHeader(null, null, 0);
    }

    return new FixtureHeader(
        Capture(GameVersionPattern, head),
        Capture(ScenarioNamePattern, head),
        CountMods(head));
  }

  private static string? Capture(Regex pattern, string head) {
    Match match = pattern.Match(head);
    return match.Success ? match.Groups[1].Value.Trim() : null;
  }

  private static int CountMods(string head) {
    Match block = ModIdsPattern.Match(head);
    return block.Success ? ListItemPattern.Matches(block.Groups[1].Value).Count : 0;
  }
}
