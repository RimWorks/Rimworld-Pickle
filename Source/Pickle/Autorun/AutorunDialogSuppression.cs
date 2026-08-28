using Verse;

namespace Pickle.Autorun;

/// <summary>
/// Drops every window opened while autorun loads a fixture, so popups cannot block
/// headless progress. Blanket rather than an allowlist, which would need maintaining.
/// </summary>
public static class AutorunDialogSuppression {
  public static bool ShouldAdd(Window window) {
    if (AutorunState.IsAutorunning && AutorunState.SuppressingFixtureLoad) {
      return false;
    }

    return true;
  }
}
