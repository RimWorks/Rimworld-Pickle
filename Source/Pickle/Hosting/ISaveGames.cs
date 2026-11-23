using System.Threading.Tasks;

namespace RimWorks.Pickle.Hosting;

/// <summary>
/// The host's saves and game sessions. Each member that returns a task completes once the
/// game has settled, so the next step finds the world it asked for.
/// </summary>
public interface ISaveGames {
  /// <summary>Loads the save at a file path.</summary>
  /// <param name="path">The full path to the save file.</param>
  /// <param name="scope">The step's wait scope, so the waits are torn down with the step.</param>
  /// <returns>A task that completes once the save is loaded and the game has settled.</returns>
  Task LoadSave(string path, object? scope);

  /// <summary>Saves the running game under a name and loads it back, so a scenario can assert state survived.
  /// Errors the game logs while it saves or loads stay in the log for the caller to judge.</summary>
  /// <param name="name">The save's name, without a file extension.</param>
  /// <param name="keepSave">When <c>false</c>, the save file is deleted once the reload finishes or fails.</param>
  /// <param name="scope">The step's wait scope, so the waits are torn down with the step.</param>
  /// <returns>A task that completes once the save has been written and loaded back.</returns>
  Task SaveAndReload(string name, bool keepSave, object? scope);

  /// <summary>Builds the world a named quickstart describes.</summary>
  /// <param name="name">The quickstart's name.</param>
  /// <param name="scope">The step's wait scope, so the waits are torn down with the step.</param>
  /// <returns>A task that completes once the world is built and the game has settled.</returns>
  Task StartQuickstart(string name, object? scope);

#pragma warning disable SA1600, CS1591
  bool SaveGame(string path, bool overwrite);
#pragma warning restore SA1600, CS1591

#pragma warning disable SA1600, CS1591
  Task ReturnToMainMenu();
#pragma warning restore SA1600, CS1591
}
