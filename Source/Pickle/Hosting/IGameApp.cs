namespace RimWorks.Pickle.Hosting;

/// <summary>The host's game process: its console log, quitting it, and opening a url.</summary>
public interface IGameApp {
  /// <summary>The file the game writes its console log to.</summary>
  string ConsoleLogPath { get; }

  /// <summary>Ends the game process with an exit code.</summary>
  /// <param name="exitCode">The code the process exits with.</param>
  void Quit(int exitCode);

  /// <summary>Opens a url with the platform's own handler, such as the default browser.</summary>
  /// <param name="url">The url to open.</param>
  void OpenUrl(string url);
}
