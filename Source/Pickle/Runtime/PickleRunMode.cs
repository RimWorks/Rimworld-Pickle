namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Controls how wait steps advance time. Watch waits for the game's tick loop; Fast
/// drives ticks manually so long waits do not take real minutes.
/// </summary>
public static class PickleRunMode {
  /// <summary>How wait steps advance the game clock.</summary>
  public enum Mode {
    /// <summary>Waits ride the game's own tick loop, at whatever speed the player set.</summary>
    Watch,

    /// <summary>Waits drive ticks manually, so a long wait finishes without taking real minutes.</summary>
    Fast,
  }

  /// <summary>The mode the current run advances waits in.</summary>
  public static Mode Current { get; set; } = Mode.Watch;
}
