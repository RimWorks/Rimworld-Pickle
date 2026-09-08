using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Blueprints and stockpile zones, so a scenario exercises real construction jobs.</summary>
[PickleSteps]
public class ConstructionSteps {
  /// <summary>Places a blueprint for a def over a rectangle of cells, without going through a designator.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="defName">The buildable def to place.</param>
  /// <param name="x1">One corner's x coordinate.</param>
  /// <param name="z1">One corner's z coordinate.</param>
  /// <param name="x2">The opposite corner's x coordinate.</param>
  /// <param name="z2">The opposite corner's z coordinate.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // The build steps place a finished building, which never exercises a blueprint, a frame or
  // the hauling job. This places what the architect menu would.
  [When("I designate a {string} from \\({int}, {int}\\) to \\({int}, {int}\\)")]
  public async Task Designate(PickleContext ctx, string defName, int x1, int z1, int x2, int z2) {
    Map map = MapLookup.RequireMap(ctx);
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    ctx.Require(def.BuildableByPlayer, $"'{defName}' is not something the player can build");

    ThingDef? stuff = def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null;
    List<IntVec3> cells = [.. CellsIn(x1, z1, x2, z2)];

    foreach (IntVec3 cell in cells) {
      MapLookup.RequireInBounds(ctx, map, cell);
      GenConstruct.PlaceBlueprintForBuild(
          def, cell, map, Rot4.North, Faction.OfPlayer, stuff, null, null, false);
    }

    await ctx.AssertEventually(
        () => cells.All(cell => BlueprintFor(map, cell, def) != null),
        () => $"no blueprint for '{defName}' landed at {DescribeBare(map, cells, def)}");
  }

  /// <summary>Places a blueprint through the real build designator, so a mod's placement rules run and any rejection surfaces as the failure message.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="defName">The buildable def to place.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // Goes through the real Designator_Build rather than placing the blueprint directly, so a
  // mod's own placement rules and their rejection reasons get exercised.
  [When("I use the build designator for {string} at \\({int}, {int}\\)")]
  public async Task DesignateWithDesignator(PickleContext ctx, string defName, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);
    ctx.Require(def.BuildableByPlayer, $"'{defName}' is not something the player can build");

    Designator_Build designator = new Designator_Build(def);
    if (def.MadeFromStuff) {
      designator.SetStuffDef(GenStuff.DefaultStuffFor(def));
    }

    AcceptanceReport report = designator.CanDesignateCell(cell);
    ctx.Require(
        report.Accepted,
        $"the build designator for '{defName}' refuses ({x}, {z}): " +
        $"{(report.Reason.NullOrEmpty() ? "no reason given" : report.Reason)}");

    designator.DesignateSingleCell(cell);

    await ctx.AssertEventually(
        () => BlueprintFor(map, cell, def) != null,
        () => $"the designator accepted ({x}, {z}) but left no blueprint for '{defName}'; " +
            $"the cell holds {MapLookup.DescribeCell(map, cell)}");
  }

  /// <summary>Asserts a blueprint for a def sits at a cell.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="defName">The buildable def expected there.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  [Then("a blueprint for {string} is at \\({int}, {int}\\)")]
  public void AssertBlueprint(PickleContext ctx, string defName, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    ctx.Assert(
        BlueprintFor(map, cell, def) != null,
        $"a blueprint for '{defName}' should be at ({x}, {z}); " +
        $"the cell holds {MapLookup.DescribeCell(map, cell)}");
  }

  /// <summary>Creates a stockpile zone over a rectangle of cells, skipping any cell that is already zoned or cannot hold a zone.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="x1">One corner's x coordinate.</param>
  /// <param name="z1">One corner's z coordinate.</param>
  /// <param name="x2">The opposite corner's x coordinate.</param>
  /// <param name="z2">The opposite corner's z coordinate.</param>
  [When("I create a stockpile from \\({int}, {int}\\) to \\({int}, {int}\\)")]
  public void CreateStockpile(PickleContext ctx, int x1, int z1, int x2, int z2) {
    Map map = MapLookup.RequireMap(ctx);
    Zone_Stockpile zone = new Zone_Stockpile(StorageSettingsPreset.DefaultStockpile, map.zoneManager);
    map.zoneManager.RegisterZone(zone);

    foreach (IntVec3 cell in CellsIn(x1, z1, x2, z2)) {
      MapLookup.RequireInBounds(ctx, map, cell);

      // The game's own designator skips these rather than erroring, and a wall in the way
      // should not fail a scenario that asked for a rectangle.
      if (map.zoneManager.ZoneAt(cell) == null && CanHoldZone(map, cell)) {
        zone.AddCell(cell);
      }
    }

    if (zone.Cells.Count == 0) {
      map.zoneManager.DeregisterZone(zone);
      ctx.Require(
          false,
          $"no cell from ({x1}, {z1}) to ({x2}, {z2}) can hold a zone; each one is already " +
          "zoned or holds something a zone cannot cover");
    }
  }

  /// <summary>Asserts a stockpile zone covers a cell.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  [Then("a stockpile covers \\({int}, {int}\\)")]
  public void AssertStockpileCovers(PickleContext ctx, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    Zone? zone = map.zoneManager.ZoneAt(cell);
    ctx.Assert(
        zone is Zone_Stockpile,
        $"a stockpile should cover ({x}, {z}); the cell is in {zone?.label ?? "no zone"}");
  }

  /// <summary>Waits for a frame at a cell to finish and become the real building.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="defName">The def the finished building should be.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // Construction has no finished event either, so this watches for the real building to
  // replace the frame.
  [When("I wait for the {string} at \\({int}, {int}\\) to be built", TimeoutSeconds = 185f)]
  public async Task WaitForBuilt(PickleContext ctx, string defName, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    await ctx.AssertEventually(
        () => cell.GetThingList(map).Any(t => t.def == def),
        () => $"no '{defName}' was built at ({x}, {z}); the cell holds " +
            $"{MapLookup.DescribeCell(map, cell)}. {DescribeProgress(map, cell)}",
        180f);
  }

  private static string DescribeBare(Map map, List<IntVec3> cells, ThingDef def) {
    List<string> bare = [.. cells
        .Where(c => BlueprintFor(map, c, def) == null)
        .Select(c => $"({c.x}, {c.z}) holding {MapLookup.DescribeCell(map, c)}")];

    return string.Join("; ", bare);
  }

  private static bool CanHoldZone(Map map, IntVec3 cell) {
    return cell.GetThingList(map).All(t => t.def.CanOverlapZones);
  }

  private static IEnumerable<IntVec3> CellsIn(int x1, int z1, int x2, int z2) {
    for (int x = System.Math.Min(x1, x2); x <= System.Math.Max(x1, x2); x++) {
      for (int z = System.Math.Min(z1, z2); z <= System.Math.Max(z1, z2); z++) {
        yield return new IntVec3(x, 0, z);
      }
    }
  }

  private static Blueprint_Build? BlueprintFor(Map map, IntVec3 cell, ThingDef def) {
    return cell.GetThingList(map)
        .OfType<Blueprint_Build>()
        .FirstOrDefault(b => b.def.entityDefToBuild == def);
  }

  // A stalled build is nearly always missing material or a colonist who will not construct.
  private static string DescribeProgress(Map map, IntVec3 cell) {
    Frame? frame = cell.GetThingList(map).OfType<Frame>().FirstOrDefault();
    if (frame != null) {
      return $"a frame stands there at {frame.PercentComplete:P0}, needing {DescribeNeeds(frame)}";
    }

    bool blueprint = cell.GetThingList(map).OfType<Blueprint>().Any();
    return blueprint ? "a blueprint is still waiting for material" : "nothing is queued there";
  }

  private static string DescribeNeeds(Frame frame) {
    List<string> needs = [.. frame.TotalMaterialCost().Select(c => $"{c.count} {c.thingDef.defName}")];
    return needs.Count == 0 ? "nothing" : string.Join(", ", needs);
  }
}
