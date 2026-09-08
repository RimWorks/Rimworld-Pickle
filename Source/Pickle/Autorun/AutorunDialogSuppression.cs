using Verse;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Drops every window opened while autorun loads a fixture, so popups cannot block
/// headless progress. Blanket rather than an allowlist, which would need maintaining.
/// </summary>
public static class AutorunDialogSuppression {
  /// <summary>Whether a window opened right now should be allowed onto the stack.</summary>
  /// <param name="window">The window about to be added. Unused, since suppression is blanket.</param>
  /// <returns><c>false</c> while autorun is loading a fixture, <c>true</c> otherwise.</returns>
  public static bool ShouldAdd(Window window) {
    if (AutorunState.IsAutorunning && AutorunState.SuppressingFixtureLoad) {
      return false;
    }

    return true;
  }
}
