using System;
using System.IO;

namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>Backs the save-file load step, so a scenario can load a file it saved itself rather than
/// a fixture shipped with a mod.</summary>
public static class SaveFilePath {
  /// <summary>The extension of the host's save files, with the leading dot, such as <c>.acme</c>.
  /// Fixtures and save file references use it. A host that never sets it gets <c>.rws</c>.</summary>
  /// <exception cref="ArgumentException">The value is empty, has no leading dot, or holds a path separator.</exception>
  public static string Extension { get; set => field = Checked(value); } = ".rws";

  /// <summary>Resolves a save file reference. A rooted path is taken as written, anything else is read
  /// from the saved games folder, and a missing extension becomes <see cref="Extension"/>.</summary>
  /// <param name="reference">What the scenario wrote: a bare save name, a file name, or a full path.</param>
  /// <param name="savedGamesFolder">The folder a bare name is read from.</param>
  /// <returns>The full path, or an empty string when the reference is blank.</returns>
  public static string Resolve(string? reference, string savedGamesFolder) {
    string trimmed = reference?.Trim() ?? string.Empty;
    if (trimmed.Length == 0) {
      return string.Empty;
    }

    string withExtension = Path.GetExtension(trimmed).Length == 0 ? trimmed + Extension : trimmed;

    return Path.IsPathRooted(withExtension)
        ? withExtension
        : Path.Combine(savedGamesFolder, withExtension);
  }

  private static string Checked(string value) {
    if (string.IsNullOrWhiteSpace(value) || value.Length < 2 || value[0] != '.' || value != value.Trim()
        || value.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 || value.IndexOfAny(['/', '\\', ':']) >= 0) {
      throw new ArgumentException("Enter a save file extension with a leading dot and no path separators.", nameof(value));
    }

    return value;
  }
}
