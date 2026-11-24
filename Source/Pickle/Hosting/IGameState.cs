namespace RimWorks.Pickle.Hosting;

/// <summary>What the host's game is doing right now, and where it keeps its saves.</summary>
public interface IGameState {
  /// <summary>Whether a game is loaded and being played.</summary>
  bool IsInPlay { get; }

  /// <summary>Whether the main menu is up.</summary>
  bool IsMainMenuUp { get; }

  /// <summary>Whether the game is busy with a long load, or has one waiting to start.</summary>
  bool IsBusy { get; }

  /// <summary>The folder the game keeps its saves in.</summary>
  string SavesFolder { get; }

#pragma warning disable SA1600, CS1591
  string SaveDataFolder { get; }
#pragma warning restore SA1600, CS1591
}
