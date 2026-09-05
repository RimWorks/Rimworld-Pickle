using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>
/// Every fixture a suite can see, from both fixture directories. Both copies of a clashing
/// name are listed, so the fixture manager can show the loser rather than only mention it.
/// </summary>
public static class FixtureCatalog {
  public static string PathForName(string directory, string name) {
    if (string.IsNullOrWhiteSpace(name) || name != name.Trim() || name == "." || name == ".."
        || name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || name.IndexOfAny(['/', '\\', ':']) >= 0) {
      throw new ArgumentException("Enter a fixture name without path separators.", nameof(name));
    }

    return Path.Combine(directory, name + ".rws");
  }

  // The writable copy wins on a name clash: re-recording a fixture is how you fix one, and
  // the committed copy would otherwise keep shadowing the new file.
  public static List<FixtureEntry> Read(string committedDir, string writableDir) {
    Dictionary<string, string> committed = new(StringComparer.OrdinalIgnoreCase);
    foreach (string path in FindRws(committedDir)) {
      committed[Path.GetFileNameWithoutExtension(path)] = path;
    }

    List<FixtureEntry> entries = [];

    if (writableDir != committedDir) {
      foreach (string path in FindRws(writableDir)) {
        string name = Path.GetFileNameWithoutExtension(path);
        committed.TryGetValue(name, out string? hidden);
        entries.Add(Describe(path, isRecorded: true, shadowedPath: hidden, isShadowed: false));

        if (hidden != null) {
          entries.Add(Describe(hidden, isRecorded: false, shadowedPath: null, isShadowed: true));
          committed.Remove(name);
        }
      }
    }

    foreach (string path in committed.Values) {
      entries.Add(Describe(path, isRecorded: false, shadowedPath: null, isShadowed: false));
    }

    return [.. entries.OrderBy(e => e.FullPath, StringComparer.Ordinal)];
  }

  private static FixtureEntry Describe(string path, bool isRecorded, string? shadowedPath, bool isShadowed) {
    FileInfo info = new FileInfo(path);
    return new FixtureEntry(
        Path.GetFileNameWithoutExtension(path),
        path,
        isRecorded,
        shadowedPath,
        isShadowed,
        info.Exists ? info.Length : 0L,
        info.Exists ? info.LastWriteTime : DateTime.MinValue);
  }

  private static List<string> FindRws(string directory) {
    if (!Directory.Exists(directory)) {
      return [];
    }

    return [.. Directory.GetFiles(directory, "*.rws", SearchOption.TopDirectoryOnly).OrderBy(f => f)];
  }
}
