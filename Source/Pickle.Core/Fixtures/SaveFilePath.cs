using System.IO;

namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>Backs the save-file load step, so a scenario can load a file it saved itself rather than
/// a fixture shipped with a mod.</summary>
public static class SaveFilePath {
  /// <summary>Resolves a save file reference. A rooted path is taken as written, anything else is read
  /// from the saved games folder, and a missing <c>.rws</c> extension is added.</summary>
  /// <param name="reference">What the scenario wrote: a bare save name, a file name, or a full path.</param>
  /// <param name="savedGamesFolder">The folder a bare name is read from.</param>
  /// <returns>The full path, or an empty string when the reference is blank.</returns>
  public static string Resolve(string? reference, string savedGamesFolder) {
    string trimmed = reference?.Trim() ?? string.Empty;
    if (trimmed.Length == 0) {
      return string.Empty;
    }

    string withExtension = Path.GetExtension(trimmed).Length == 0 ? trimmed + ".rws" : trimmed;

    return Path.IsPathRooted(withExtension)
        ? withExtension
        : Path.Combine(savedGamesFolder, withExtension);
  }
}
