using System;
using System.Linq;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Setting up a map before a scenario acts on it: colonists, things, buildings, research and game speed.</summary>
[PickleSteps]
public class WorldSteps {
  /// <summary>Generates a colonist under the nickname, or does nothing when one already exists. Seeded from the scenario so a rerun spawns the same pawn.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  [Given("a colonist {string} exists")]
  public void ColonistExists(PickleContext ctx, string nickname) {
    bool alreadyExists = PawnsFinder.AllMaps_FreeColonists
        .Any(p => string.Equals(p.Name?.ToStringShort, nickname, StringComparison.OrdinalIgnoreCase));
    if (alreadyExists) {
      return;
    }

    Map map = RequireMap(ctx);

    // seeded here, not at scenario start: Rand is one stream the game draws from every tick.
    // Pop puts the game's own stream back, so generating a pawn does not shift later rolls.
    Rand.PushState(Gen.HashCombineInt(ctx.ScenarioSeed, GenText.StableStringHash(nickname)));

    try {
      Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
      pawn.Name = new NameTriple(nickname, nickname, nickname);
      GenSpawn.Spawn(pawn, FindSpawnCell(ctx, map), map, WipeMode.Vanish);
    } finally {
      Rand.PopState();
    }
  }

  /// <summary>Places a stack of the def in the first stockpile zone, or the map centre when there is none.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="stackCount">How many to place in the stack.</param>
  /// <param name="defName">The thing def to make.</param>
  [Given("{int} {string} is spawned at the stockpile")]
  public void ThingSpawnedAtStockpile(PickleContext ctx, int stackCount, string defName) {
    ThingDef thingDef = RequireThingDef(defName);
    Map map = RequireMap(ctx);

    Thing thing = ThingMaker.MakeThing(thingDef, thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(thingDef) : null);
    thing.stackCount = stackCount;

    IntVec3 cell = FirstStockpileCellOrMapCenter(map);
    GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
  }

  /// <summary>Spawns a finished building owned by the player faction, skipping the blueprint and frame stages.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The building def to place.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  [Given("a {string} is built at \\({int}, {int}\\)")]
  public void BuiltAt(PickleContext ctx, string defName, int x, int z) {
    ThingDef thingDef = RequireThingDef(defName);
    Map map = RequireMap(ctx);

    Thing thing = ThingMaker.MakeThing(thingDef, thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(thingDef) : null);
    thing.SetFaction(Faction.OfPlayer);
    GenSpawn.Spawn(thing, new IntVec3(x, 0, z), map, WipeMode.Vanish);
  }

  /// <summary>Spawns any pawn kind, which the colonist step cannot: an animal, a raider or an anomaly entity. Uses the kind's default faction, or none if that faction was never generated for this game.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="kindDefName">The pawn kind def to generate.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  // The colonist step only makes colonists. An anomaly entity, an animal or a raider is a
  // PawnKindDef, and nothing could spawn one until now.
  [When("I spawn a {string} pawn at \\({int}, {int}\\)")]
  public void SpawnPawn(PickleContext ctx, string kindDefName, int x, int z) {
    Map map = RequireMap(ctx);
    PawnKindDef kind = DefLookup.Require<PawnKindDef>(kindDefName);
    IntVec3 cell = new IntVec3(x, 0, z);

    ctx.Require(
        cell.InBounds(map),
        $"cell ({x}, {z}) is outside the map, which is {map.Size.x} by {map.Size.z}");

    Faction? faction = kind.defaultFactionDef == null
        ? null
        : Find.FactionManager.FirstFactionOfDef(kind.defaultFactionDef);
    Pawn pawn = PawnGenerator.GeneratePawn(kind, faction);
    GenSpawn.Spawn(pawn, cell, map);
  }

  /// <summary>Marks a research project finished, with no completion dialog or letter.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The research project def.</param>
  [Given("research {string} is finished")]
  public void ResearchFinished(PickleContext ctx, string defName) {
    ResearchProjectDef project = RequireResearchProjectDef(defName);
    Find.ResearchManager.FinishProject(project, doCompletionDialog: false, researcher: null, doCompletionLetter: false);
  }

  /// <summary>Sets the tick manager's speed. Accepts paused, normal, fast, superfast or ultrafast.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="speed">The speed name, case insensitive.</param>
  [Given("game speed is {word}")]
  public void GameSpeedIs(PickleContext ctx, string speed) {
    TimeSpeed timeSpeed = speed.ToLowerInvariant() switch {
      "paused" => TimeSpeed.Paused,
      "normal" => TimeSpeed.Normal,
      "fast" => TimeSpeed.Fast,
      "superfast" => TimeSpeed.Superfast,
      "ultrafast" => TimeSpeed.Ultrafast,
      _ => throw new ArgumentException(
          $"unknown game speed '{speed}'; supported: paused, normal, fast, superfast, ultrafast"),
    };

    Find.TickManager.CurTimeSpeed = timeSpeed;
  }

  // map.Center is inside the mountain on plenty of maps, and a colonist standing in rock
  // cannot path anywhere, so every movement step downstream fails for no visible reason
  private static IntVec3 FindSpawnCell(PickleContext ctx, Map map) {
    IntVec3 origin = map.mapPawns.FreeColonistsSpawned.FirstOrDefault()?.Position ?? map.Center;
    if (CellFinder.TryFindRandomCellNear(origin, map, 20, c => c.Standable(map), out IntVec3 near, 200)) {
      return near;
    }

    ctx.Require(
        CellFinder.TryFindRandomCellNear(map.Center, map, 80, c => c.Standable(map), out IntVec3 wide, 500),
        "no standable cell found on this map to spawn a colonist into");

    return wide;
  }

  private static Map RequireMap(PickleContext ctx) {
    Map? map = Find.CurrentMap;
    ctx.Require(map != null, "no current map is loaded; load a save first with 'the save ... is loaded'");
    return map!;
  }

  private static IntVec3 FirstStockpileCellOrMapCenter(Map map) {
    Zone_Stockpile? stockpile = map.zoneManager.AllZones.OfType<Zone_Stockpile>().FirstOrDefault();
    if (stockpile != null && stockpile.Cells.Count > 0) {
      return stockpile.Cells[0];
    }

    return map.Center;
  }

  private static ThingDef RequireThingDef(string defName) {
    return DefLookup.Require<ThingDef>(defName);
  }

  private static ResearchProjectDef RequireResearchProjectDef(string defName) {
    return DefLookup.Require<ResearchProjectDef>(defName);
  }
}
