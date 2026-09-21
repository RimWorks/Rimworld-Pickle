using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorks.Pickle;

/// <summary>Matches a mod by name or packageId, the same rule <c>@requires:</c> tags use, so a
/// scenario's tag and a `mod ... is loaded` step can never disagree.</summary>
public static class ModLookup {
  /// <summary>Whether a mod matching <paramref name="wanted"/> by name or packageId is loaded.</summary>
  /// <param name="wanted">The mod name or packageId, case insensitive.</param>
  /// <returns><c>true</c> when a loaded mod matches.</returns>
  public static bool IsLoaded(string wanted) {
    return IndexOf(wanted) != null;
  }

  /// <summary>The mod's position in load order, or <c>null</c> when no mod matches.</summary>
  /// <param name="wanted">The mod name or packageId, case insensitive.</param>
  /// <returns>The zero-based index into <see cref="LoadedModManager.RunningModsListForReading"/>, or
  /// <c>null</c> when no mod matches.</returns>
  public static int? IndexOf(string wanted) {
    List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
    for (int i = 0; i < mods.Count; i++) {
      if (Matches(mods[i], wanted)) {
        return i;
      }
    }

    return null;
  }

  /// <summary>The loaded mod matching a name or packageId, or <c>null</c> when none does.</summary>
  /// <param name="wanted">The mod name or packageId, case insensitive.</param>
  /// <returns>The matching mod, or <c>null</c>.</returns>
  public static ModContentPack? Find(string wanted) {
    int? index = IndexOf(wanted);
    return index == null ? null : LoadedModManager.RunningModsListForReading[index.Value];
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

  /// <summary>Every loaded mod's name and packageId, in load order, for a miss message.</summary>
  /// <returns>The loaded mods, comma separated.</returns>
  public static string DescribeLoadOrder() {
    return string.Join(
        ", ", LoadedModManager.RunningModsListForReading.Select(m => $"{m.Name} ({m.PackageIdPlayerFacing})"));
  }
}
