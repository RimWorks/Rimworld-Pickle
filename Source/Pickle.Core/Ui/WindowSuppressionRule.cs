using System;

namespace RimWorks.Pickle.Core.Ui;

/// <summary>
/// Decides whether a window opening right now is allowed onto the stack. Split from the
/// Verse-facing side so the rule itself stays unit testable: getting this wrong in the "drop"
/// direction leaves the game unable to open anything at all.
/// </summary>
public static class WindowSuppressionRule {
  private const string OwnAssemblyPrefix = "RimWorks.Pickle";

  /// <summary>Whether a window from the named assembly may open.</summary>
  /// <param name="active">Whether suppression is currently on.</param>
  /// <param name="assemblyName">The simple name of the assembly declaring the window's type.</param>
  /// <returns><c>true</c> to let the window through, <c>false</c> to drop it.</returns>
  // An unknown assembly is let through. A window whose origin cannot be read is far more likely
  // to be something the player needs than something worth dropping, and the cost of the two
  // mistakes is not symmetric.
  public static bool Allows(bool active, string? assemblyName) {
    if (!active || assemblyName == null) {
      return true;
    }

    return IsOwn(assemblyName);
  }

  /// <summary>Whether the runner itself owns the assembly, and so must never have its windows dropped.</summary>
  /// <param name="assemblyName">The simple name of the assembly to judge.</param>
  /// <returns><c>true</c> when the assembly is one of Pickle's own.</returns>
  public static bool IsOwn(string? assemblyName) {
    return assemblyName != null
        && assemblyName.StartsWith(OwnAssemblyPrefix, StringComparison.Ordinal);
  }
}
