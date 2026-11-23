using RimWorks.Pickle.Hosting;
using Verse;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class GameState : IGameState {
  public bool IsInPlay => Current.Game != null && Current.ProgramState == ProgramState.Playing;

  public bool IsMainMenuUp => Current.ProgramState == ProgramState.Entry;

  public bool IsBusy => LongEventHandler.AnyEventNowOrWaiting;

  public string SavesFolder => GenFilePaths.SavedGamesFolderPath;

  public string SaveDataFolder => GenFilePaths.SaveDataFolderPath;
}
