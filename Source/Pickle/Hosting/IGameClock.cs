namespace RimWorks.Pickle.Hosting;

/// <summary>The host's game clock, which tick waits read and watch mode unpauses.</summary>
public interface IGameClock {
  /// <summary>Whether a game is running, so it has a clock to read.</summary>
  bool IsGameRunning { get; }

  /// <summary>How many ticks the running game has advanced.</summary>
  int Ticks { get; }

  /// <summary>Whether the running game's clock is stopped.</summary>
  bool IsPaused { get; }

  /// <summary>Whether the game holds a pause of its own that the runner must not lift.</summary>
  bool IsForcePaused { get; }

  /// <summary>Runs the running game's clock at normal speed.</summary>
  void SetNormalSpeed();
}
