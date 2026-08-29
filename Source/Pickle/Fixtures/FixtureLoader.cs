using System;
using System.IO;
using System.Threading.Tasks;
using Pickle.Runtime;
using Verse;

namespace Pickle.Fixtures;

public static class FixtureLoader {
  public static async Task LoadFixture(string resolvedRwsPath, PickleDriver driver, object? scope = null) {
    string savedGamesFolder = GenFilePaths.SavedGamesFolderPath;
    string tempRwsPath = Path.Combine(savedGamesFolder, "__pickle_fixture.rws");

    try {
      File.Copy(resolvedRwsPath, tempRwsPath, overwrite: true);

      Game? gameBeforeLoad = Current.Game;
      GameDataSaveLoader.LoadGame("__pickle_fixture");

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
      try {
        if (File.Exists(tempRwsPath)) {
          File.Delete(tempRwsPath);
        }
      } catch {
        Log.Warning("Failed to delete temporary fixture file: " + tempRwsPath);
      }
    }
  }
}
