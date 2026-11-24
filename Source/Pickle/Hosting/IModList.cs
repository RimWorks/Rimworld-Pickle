using System.Collections.Generic;

namespace RimWorks.Pickle.Hosting;

/// <summary>The mods the host's game is running, which suite discovery and the mod steps read.</summary>
public interface IModList {
  /// <summary>Every running mod, in load order.</summary>
  IReadOnlyList<RunningMod> RunningMods { get; }
}
