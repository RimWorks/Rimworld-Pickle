using RimWorks.Pickle.Core.Ui;
using Verse;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Keeps foreign windows off the stack for the length of a scenario. Closing them once is not
/// enough: a log viewer that tails errors, or a mod that reopens its notice, is back on the next
/// frame and swallows the click the scenario was about to make.
/// </summary>
public static class WindowSuppression {
  /// <summary>Whether foreign windows are currently being dropped as they open.</summary>
  public static bool Active { get; private set; }

  /// <summary>Starts dropping foreign windows as they open.</summary>
  public static void Begin() {
    Active = true;
  }

  /// <summary>Stops dropping foreign windows, so the game behaves normally again.</summary>
  // Called from an [AfterScenario] hook as well as from the step, so a scenario that dies
  // between the two does not leave the player's game unable to open anything.
  public static void End() {
    Active = false;
  }

  /// <summary>Whether the runner owns the window, and so must never drop it.</summary>
  /// <param name="window">The window to judge.</param>
  /// <returns><c>true</c> when the window belongs to Pickle itself.</returns>
  public static bool IsOwn(Window window) {
    return WindowSuppressionRule.IsOwn(AssemblyNameOf(window));
  }

  /// <summary>Whether a window opening right now should be allowed onto the stack.</summary>
  /// <param name="window">The window about to be added.</param>
  /// <returns><c>false</c> to drop the window instead of adding it.</returns>
  public static bool ShouldAdd(Window window) {
    return WindowSuppressionRule.Allows(Active, AssemblyNameOf(window));
  }

  private static string? AssemblyNameOf(Window window) {
    return window?.GetType().Assembly.GetName().Name;
  }
}
