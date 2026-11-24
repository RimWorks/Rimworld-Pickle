using RimWorks.Pickle.Runtime;
using Verse;

namespace RimWorks.Pickle.Autorun;

/// <summary>
/// Drops windows opened while autorun loads a fixture, so popups cannot block headless progress.
/// Only a window the scenario registered an answer for is let through.
/// </summary>
public static class AutorunDialogSuppression {
  /// <summary>Whether a window opened right now should be allowed onto the stack.</summary>
  /// <param name="window">The window about to be added.</param>
  /// <returns><c>true</c> outside a fixture load, or when the scenario asked for this window to be answered.</returns>
  public static bool ShouldAdd(Window window) {
    if (AutorunState.IsAutorunning && AutorunState.SuppressingFixtureLoad) {
      return DialogAnswering.Wants(window);
    }

    return true;
  }
}
