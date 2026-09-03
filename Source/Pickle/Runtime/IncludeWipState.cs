using RimWorks.Pickle.Autorun;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Whether a run includes scenarios tagged @wip. Every run path reads this, so the toggle
/// in the runner window and the dashboard reaches an autorun too.
/// </summary>
public static class IncludeWipState {
  // Seeded on first read rather than from a startup hook. The dashboard answers requests
  // before StaticConstructorOnStartup finishes, so a hook that assigned this could land
  // after a /wip call and quietly undo it.
  public static bool Enabled { get; set; } = PickleArgs.Parse().IncludeWip;
}
