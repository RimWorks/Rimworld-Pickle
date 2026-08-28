namespace Pickle.Runtime;

/// <summary>
/// Controls how wait steps advance time. Watch waits for the game's tick loop; Fast
/// drives ticks manually so long waits do not take real minutes.
/// </summary>
public static class PickleRunMode {
  public enum Mode {
    Watch,
    Fast,
  }

  public static Mode Current { get; set; } = Mode.Watch;
}
