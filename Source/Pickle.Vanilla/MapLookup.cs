using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Pickle.Vanilla;

internal static class MapLookup {
  public static Map RequireMap(PickleContext ctx) {
    Map? map = Find.CurrentMap;
    ctx.Require(map != null, "no current map is loaded; load a save first with 'the save ... is loaded'");
    return map!;
  }

  public static void RequireInBounds(PickleContext ctx, Map map, IntVec3 cell) {
    ctx.Require(
        cell.InBounds(map),
        $"cell ({cell.x}, {cell.z}) is outside the map, which is {map.Size.x} by {map.Size.z}");
  }

  public static Thing RequireThingAt(PickleContext ctx, Map map, IntVec3 cell, ThingDef def) {
    RequireInBounds(ctx, map, cell);

    Thing? thing = cell.GetThingList(map).FirstOrDefault(t => t.def == def);
    ctx.Require(
        thing != null,
        $"no {def.defName} at ({cell.x}, {cell.z}); cell holds: {DescribeCell(map, cell)}");

    return thing!;
  }

  public static string DescribeCell(Map map, IntVec3 cell) {
    List<string> labels = [.. cell.GetThingList(map).Select(t => t.def.defName)];
    return labels.Count == 0 ? "(nothing)" : string.Join(", ", labels);
  }
}
