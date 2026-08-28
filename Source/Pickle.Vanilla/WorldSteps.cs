using System;
using System.Linq;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

[PickleSteps]
public class WorldSteps {
  [Given("a colonist {string} exists")]
  public void ColonistExists(PickleContext ctx, string nickname) {
    bool alreadyExists = PawnsFinder.AllMaps_FreeColonists
        .Any(p => string.Equals(p.Name?.ToStringShort, nickname, StringComparison.OrdinalIgnoreCase));
    if (alreadyExists) {
      return;
    }

    Map map = RequireMap(ctx);
    Pawn pawn = PawnGenerator.GeneratePawn(PawnKindDefOf.Colonist, Faction.OfPlayer);
    pawn.Name = new NameTriple(nickname, nickname, nickname);
    GenSpawn.Spawn(pawn, map.Center, map, WipeMode.Vanish);
  }

  [Given("{int} {string} is spawned at the stockpile")]
  public void ThingSpawnedAtStockpile(PickleContext ctx, int stackCount, string defName) {
    ThingDef thingDef = RequireThingDef(defName);
    Map map = RequireMap(ctx);

    Thing thing = ThingMaker.MakeThing(thingDef, thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(thingDef) : null);
    thing.stackCount = stackCount;

    IntVec3 cell = FirstStockpileCellOrMapCenter(map);
    GenPlace.TryPlaceThing(thing, cell, map, ThingPlaceMode.Near);
  }

  [Given("a {string} is built at \\({int}, {int}\\)")]
  public void BuiltAt(PickleContext ctx, string defName, int x, int z) {
    ThingDef thingDef = RequireThingDef(defName);
    Map map = RequireMap(ctx);

    Thing thing = ThingMaker.MakeThing(thingDef, thingDef.MadeFromStuff ? GenStuff.DefaultStuffFor(thingDef) : null);
    thing.SetFaction(Faction.OfPlayer);
    GenSpawn.Spawn(thing, new IntVec3(x, 0, z), map, WipeMode.Vanish);
  }

  [Given("research {string} is finished")]
  public void ResearchFinished(PickleContext ctx, string defName) {
    ResearchProjectDef project = RequireResearchProjectDef(defName);
    Find.ResearchManager.FinishProject(project, doCompletionDialog: false, researcher: null, doCompletionLetter: false);
  }

  [Given("game speed is {word}")]
  public void GameSpeedIs(PickleContext ctx, string speed) {
    TimeSpeed timeSpeed = speed.ToLowerInvariant() switch {
      "paused" => TimeSpeed.Paused,
      "normal" => TimeSpeed.Normal,
      "fast" => TimeSpeed.Fast,
      "superfast" => TimeSpeed.Superfast,
      _ => throw new ArgumentException(
          $"unknown game speed '{speed}'; supported: paused, normal, fast, superfast"),
    };

    Find.TickManager.CurTimeSpeed = timeSpeed;
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
