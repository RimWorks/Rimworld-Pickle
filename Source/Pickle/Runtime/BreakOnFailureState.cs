namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Set by the break-on-failure toggle. RunSession reads it to decide whether a failed
/// step pauses the run.
/// </summary>
public static class BreakOnFailureState {
  public static bool Enabled { get; set; }
}
