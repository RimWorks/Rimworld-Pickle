using System.IO;
using System.Linq;
using RimWorks.Pickle.Core.Fixtures;
using RimWorks.Pickle.Hosting;

namespace RimWorks.Pickle.Runtime;

/// <summary>Puts another save file extension in effect for one scenario, so a scenario can check fixtures follow it.</summary>
[PickleSteps]
public static class SaveExtensionSteps {
  private static bool armed;
  private static string previous = string.Empty;
  private static string? written;

  /// <summary>Sets the save file extension until the scenario ends.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="extension">The extension to use, with the leading dot.</param>
  [Given("the save file extension is {string}")]
  public static void Use(PickleContext ctx, string extension) {
    if (!armed) {
      previous = SaveFilePath.Extension;
      armed = true;
    }

    SaveFilePath.Extension = extension;
    ctx.Assert(SaveFilePath.Extension == extension, $"the save file extension is {SaveFilePath.Extension}, not {extension}");
  }

  /// <summary>Saves the running game as a fixture by name, in the saved games folder.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="name">The fixture name, without an extension.</param>
  [When("I save fixture {string} to the saved games folder")]
  public static void SaveFixture(PickleContext ctx, string name) {
    string path = FixtureCatalog.PathForName(SavesFolder(), name);
    if (armed) {
      written = path;
    }

    ctx.Assert(HostRegistry.Get<ISaveGames>().SaveGame(path, true), $"the host did not save fixture {name} to {path}");
  }

  /// <summary>Checks a fixture saved by name is a file with an extension.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="name">The fixture name, without an extension.</param>
  /// <param name="extension">The extension the file must have, with the leading dot.</param>
  [Then("fixture {string} in the saved games folder is a {string} file")]
  public static void IsFile(PickleContext ctx, string name, string extension) {
    string expected = Path.Combine(SavesFolder(), name + extension);
    ctx.Assert(File.Exists(expected), $"no file at {expected}");
    ctx.Assert(FixtureCatalog.PathForName(SavesFolder(), name) == expected, $"fixture {name} does not map to {expected}");
  }

  /// <summary>Checks the fixture list of the saved games folder has a fixture, as a file with an extension.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="name">The fixture name, without an extension.</param>
  /// <param name="extension">The extension the listed file must have, with the leading dot.</param>
  [Then("the saved games folder lists fixture {string} as a {string} file")]
  public static void IsListed(PickleContext ctx, string name, string extension) {
    string folder = SavesFolder();
    string expected = Path.Combine(folder, name + extension);
    ctx.Assert(
        FixtureCatalog.Read(folder, folder).Any(e => e.Name == name && e.FullPath == expected),
        $"the fixture list of {folder} has no {expected}");
  }

  /// <summary>Checks a save file reference without an extension resolves to a file with an extension.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="reference">The save file reference, without an extension.</param>
  /// <param name="extension">The extension the resolved path must have, with the leading dot.</param>
  [Then("save file {string} resolves to a {string} file in the saved games folder")]
  public static void Resolves(PickleContext ctx, string reference, string extension) {
    string expected = Path.Combine(SavesFolder(), reference + extension);
    string resolved = SaveFilePath.Resolve(reference, SavesFolder());
    ctx.Assert(resolved == expected, $"save file {reference} resolves to {resolved}, not {expected}");
  }

  /// <summary>Puts the earlier save file extension back and deletes the fixture the scenario saved.</summary>
  [AfterScenario]
  public static void Restore() {
    if (!armed) {
      return;
    }

    armed = false;
    SaveFilePath.Extension = previous;
    if (written != null && File.Exists(written)) {
      File.Delete(written);
    }

    written = null;
  }

  private static string SavesFolder() {
    return HostRegistry.Get<IGameState>().SavesFolder;
  }
}
