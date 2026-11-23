using System;
using Verse;

namespace RimWorks.Pickle;

/// <summary>Resolves a loaded mod's <see cref="ModContentPack"/> by name or packageId, with the same
/// rule as <see cref="ModLookup"/>, for steps that need the game's own mod object.</summary>
public static class ModPackLookup {
  /// <summary>The loaded mod matching a name or packageId, or <c>null</c> when none does.</summary>
  /// <param name="wanted">The mod name or packageId, case insensitive.</param>
  /// <returns>The matching mod, or <c>null</c>.</returns>
  public static ModContentPack? Find(string wanted) {
    foreach (ModContentPack mod in LoadedModManager.RunningModsListForReading) {
      if (Matches(mod, wanted)) {
        return mod;
      }
    }

    return null;
  }

  /// <summary>Whether a mod matches a name or packageId, also checking <see cref="ModContentPack.PackageIdPlayerFacing"/>
  /// since a Steam copy's <see cref="ModContentPack.PackageId"/> carries a <c>_steam</c> postfix.</summary>
  /// <param name="mod">The mod to test.</param>
  /// <param name="wanted">The mod name or packageId, case insensitive.</param>
  /// <returns><c>true</c> when <paramref name="mod"/> matches.</returns>
  public static bool Matches(ModContentPack mod, string wanted) {
    return string.Equals(mod.Name, wanted, StringComparison.OrdinalIgnoreCase)
        || string.Equals(mod.PackageId, wanted, StringComparison.OrdinalIgnoreCase)
        || string.Equals(mod.PackageIdPlayerFacing, wanted, StringComparison.OrdinalIgnoreCase);
  }
}
