using System;
using System.IO;
using System.Threading.Tasks;
using Pickle.Autorun;
using Pickle.Runtime;
using Verse;

namespace Pickle.Fixtures;

public static class FixtureLoader {
  public static async Task LoadFixture(string resolvedRwsPath, PickleDriver driver, object? scope = null) {
    string savedGamesFolder = GenFilePaths.SavedGamesFolderPath;
    string tempRwsPath = Path.Combine(savedGamesFolder, "__pickle_fixture.rws");

    try {
      File.Copy(resolvedRwsPath, tempRwsPath, overwrite: true);
      await LoadAndSettle("__pickle_fixture", driver, scope);
    } finally {
      DeleteQuietly(tempRwsPath);
    }
  }

  /// <summary>
  /// Saves the running game and loads it straight back, so a scenario can assert that
  /// state survived the trip. Errors thrown while scribing are left in the log on
  /// purpose; the caller decides whether they fail the step.
  /// </summary>
  public static async Task SaveAndReload(string saveName, PickleDriver driver, object? scope, bool keepSave) {
    string savePath = GenFilePaths.FilePathForSavedGame(saveName);

    try {
      GameDataSaveLoader.SaveGame(saveName);

      // SaveGame swallows its own exceptions into a log line, so the file is the only
      // honest signal that it worked.
      if (!File.Exists(savePath)) {
        throw new InvalidOperationException(
            $"saving '{saveName}' wrote no file; the scribe error is in the log above");
      }

      await LoadAndSettle(saveName, driver, scope);
    } finally {
      if (!keepSave) {
        DeleteQuietly(savePath);
      }
    }
  }

  private static async Task LoadAndSettle(string saveName, PickleDriver driver, object? scope) {
    Game? gameBeforeLoad = Current.Game;

    AutorunState.SuppressingFixtureLoad = true;
    try {
      GameDataSaveLoader.LoadGame(saveName);

      await driver.WaitUntil(
          () => Current.Game != null
              && !ReferenceEquals(Current.Game, gameBeforeLoad)
              && Current.ProgramState == ProgramState.Playing
              && Find.CurrentMap != null
              && !LongEventHandler.AnyEventNowOrWaiting,
          175f,
          scope);
      await driver.WaitTicks(2, scope);

      // The map is live before the screen finishes fading in. Returning here would hand
      // the next step a half dark screen, which a click or a screenshot both care about.
      await driver.WaitUntil(() => !ScreenFader.IsFading(), 15f, scope);
    } finally {
      AutorunState.SuppressingFixtureLoad = false;
    }
  }

  private static void DeleteQuietly(string path) {
    try {
      if (File.Exists(path)) {
        File.Delete(path);
      }
    } catch {
      Log.Warning("pickle: failed to delete temporary save file: " + path);
    }
  }
}
