using System.Collections.Generic;
using RimWorks.Pickle.Core.Ui;
using Verse;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Keeps foreign windows off the stack for the length of a scenario. Closing them once is not
/// enough: a log viewer that tails errors, or a mod that reopens its notice, is back on the next
/// frame and swallows the click the scenario was about to make.
/// </summary>
public static class WindowSuppression {
  private static readonly HashSet<Window> Pending = [];

  /// <summary>Whether foreign windows are currently being dropped as they open.</summary>
  public static bool Active { get; private set; }

  /// <summary>How many runner-owned windows are registered and still waiting to open.</summary>
  public static int PendingCount => Pending.Count;

  /// <summary>Starts dropping foreign windows as they open.</summary>
  public static void Begin() {
    Active = true;
  }

  /// <summary>Stops dropping foreign windows, so the game behaves normally again.</summary>
  // Called from RunSession's finally as well as from the step, so a scenario that dies between
  // the two does not leave the player's game unable to open anything.
  public static void End() {
    Active = false;
  }

  /// <summary>Opens a window the runner owns, so suppression cannot drop it.</summary>
  /// <param name="window">The window to open.</param>
  // The runner opens vanilla types, FloatMenu and Dialog_MessageBox among them, and those declare
  // in Assembly-CSharp. The assembly name cannot tell them from a foreign mod's window, so the
  // instance is registered here instead and ShouldAdd consumes the registration.
  public static void AddOwn(Window window) {
    Pending.Add(window);
    Find.WindowStack.Add(window);
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
    if (window == null) {
      return true;
    }

    return Pending.Remove(window)
        || DialogAnswering.Wants(window)
        || WindowSuppressionRule.Allows(Active, AssemblyNameOf(window));
  }

  private static string? AssemblyNameOf(Window window) {
    return window?.GetType().Assembly.GetName().Name;
  }
}
