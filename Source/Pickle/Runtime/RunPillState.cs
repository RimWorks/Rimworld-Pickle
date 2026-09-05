namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Whether a run shows the progress pill. RestorePill reads it every frame, so the
/// toggle lands mid-run rather than at the next one.
/// </summary>
public static class RunPillState {
  public static bool Enabled { get; set; } = true;
}
