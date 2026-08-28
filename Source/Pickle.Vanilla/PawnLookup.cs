using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

internal static class PawnLookup {
  public static Pawn RequireLiving(string nickname) {
    Pawn? pawn = FindLiving(nickname);
    if (pawn != null) {
      return pawn;
    }

    throw new InvalidOperationException(DescribeMissing(nickname));
  }

  public static Pawn RequireLivingOrDead(string nickname, Map map) {
    Pawn? pawn = FindLiving(nickname) ?? FindCorpse(nickname, map);
    if (pawn != null) {
      return pawn;
    }

    throw new InvalidOperationException(DescribeMissing(nickname));
  }

  private static Pawn? FindLiving(string nickname) {
    return PawnsFinder.AllMaps_FreeColonists
        .FirstOrDefault(p => string.Equals(p.Name?.ToStringShort, nickname, StringComparison.OrdinalIgnoreCase));
  }

  private static Pawn? FindCorpse(string nickname, Map map) {
    return map.listerThings.ThingsInGroup(ThingRequestGroup.Corpse)
        .OfType<Corpse>()
        .Select(c => c.InnerPawn)
        .FirstOrDefault(p => string.Equals(p?.Name?.ToStringShort, nickname, StringComparison.OrdinalIgnoreCase));
  }

  private static string DescribeMissing(string requested) {
    List<string> names = [.. PawnsFinder.AllMaps_FreeColonists
        .Select(p => p.Name?.ToStringShort ?? p.LabelShort)
        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)];

    string available = names.Count == 0 ? "(none)" : string.Join(", ", names);
    return $"no pawn nicknamed '{requested}'. player pawns present: {available}";
  }
}
