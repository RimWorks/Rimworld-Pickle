namespace Pickle.Autorun;

/// <summary>
/// Both flags must be true for a dialog to be dropped, so suppression applies only
/// during autorun's own fixture loads.
/// </summary>
public static class AutorunState {
  public static bool IsAutorunning { get; set; }

  public static bool SuppressingFixtureLoad { get; set; }
}
