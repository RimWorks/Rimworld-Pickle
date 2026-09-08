namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Both flags must be true for a dialog to be dropped, so suppression applies only
/// during autorun's own fixture loads.
/// </summary>
public static class AutorunState {
  /// <summary>Whether the current process is running scenarios unattended, rather than at the interactive UI.</summary>
  public static bool IsAutorunning { get; set; }

  /// <summary>Whether a fixture is loading right now, the window the dialog suppression actually needs.</summary>
  public static bool SuppressingFixtureLoad { get; set; }
}
