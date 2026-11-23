using System;
using System.Collections.Generic;
using System.Linq;
using RimWorks.Pickle.Hosting;

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
  /// <returns>The zero-based index into <see cref="IModList.RunningMods"/>, or
  /// <c>null</c> when no mod matches.</returns>
  public static int? IndexOf(string wanted) {
    IReadOnlyList<RunningMod> mods = HostRegistry.Get<IModList>().RunningMods;
    for (int i = 0; i < mods.Count; i++) {
      RunningMod mod = mods[i];
      if (string.Equals(mod.Name, wanted, StringComparison.OrdinalIgnoreCase)
          || string.Equals(mod.PackageId, wanted, StringComparison.OrdinalIgnoreCase)
          || string.Equals(mod.PackageIdPlayerFacing, wanted, StringComparison.OrdinalIgnoreCase)) {
        return i;
      }
    }

    return null;
  }

  /// <summary>Every loaded mod's name and packageId, in load order, for a miss message.</summary>
  /// <returns>The loaded mods, comma separated.</returns>
  public static string DescribeLoadOrder() {
    return string.Join(
        ", ", HostRegistry.Get<IModList>().RunningMods.Select(m => $"{m.Name} ({m.PackageIdPlayerFacing})"));
  }
}
