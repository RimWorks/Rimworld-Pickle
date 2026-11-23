using System.Collections.Generic;
using RimWorks.Pickle.Hosting;
using Verse;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class ModList : IModList {
  public IReadOnlyList<RunningMod> RunningMods {
    get {
      List<ModContentPack> mods = LoadedModManager.RunningModsListForReading;
      List<RunningMod> running = new List<RunningMod>(mods.Count);
      foreach (ModContentPack mod in mods) {
        running.Add(new RunningMod(mod.Name, mod.PackageId, mod.PackageIdPlayerFacing, mod.RootDir));
      }

      return running;
    }
  }
}
