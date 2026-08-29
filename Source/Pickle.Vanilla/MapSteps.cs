using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace Pickle.Vanilla;

/// <summary>
/// What is on the map and where. Covers spawn and despawn testing, which asserting
/// on pawn state cannot reach.
/// </summary>
[PickleSteps]
public class MapSteps {
  [Then("a {string} exists")]
  public async Task AssertThingExists(PickleContext ctx, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    await ctx.AssertEventually(
        () => map.listerThings.ThingsOfDef(def).Count > 0,
        () => $"expected at least one {defName} on the map; found none");
  }

  [Then("no {string} exists")]
  public void AssertThingAbsent(PickleContext ctx, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    int count = map.listerThings.ThingsOfDef(def).Count;
    ctx.Assert(count == 0, $"expected no {defName} on the map; found {count}");
  }

  [Then("{int} {string} exist")]
  public void AssertThingCount(PickleContext ctx, int expected, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    int actual = map.listerThings.ThingsOfDef(def).Sum(t => t.stackCount);
    ctx.Assert(actual == expected, $"expected {expected} {defName}; found {actual}");
  }

  [Then("a {string} is at \\({int}, {int}\\)")]
  public void AssertThingAtCell(PickleContext ctx, string defName, int x, int z) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    ctx.Assert(
        cell.GetThingList(map).Any(t => t.def == def),
        $"expected a {defName} at ({x}, {z}); cell holds: {MapLookup.DescribeCell(map, cell)}");
  }

  [Then("no {string} is at \\({int}, {int}\\)")]
  public void AssertThingNotAtCell(PickleContext ctx, string defName, int x, int z) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    ctx.Assert(
        !cell.GetThingList(map).Any(t => t.def == def),
        $"expected no {defName} at ({x}, {z}); cell holds: {MapLookup.DescribeCell(map, cell)}");
  }

  [Then("cell \\({int}, {int}\\) is empty")]
  public void AssertCellEmpty(PickleContext ctx, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    List<Thing> things = cell.GetThingList(map);
    ctx.Assert(things.Count == 0, $"cell ({x}, {z}) should be empty; holds: {MapLookup.DescribeCell(map, cell)}");
  }

  [When("I spawn a {string} at \\({int}, {int}\\)")]
  public void SpawnAtCell(PickleContext ctx, string defName, int x, int z) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
    GenSpawn.Spawn(thing, cell, map);
  }

  [When("I destroy the {string} at \\({int}, {int}\\)")]
  public void DestroyAtCell(PickleContext ctx, string defName, int x, int z) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    MapLookup.RequireThingAt(ctx, map, new IntVec3(x, 0, z), def).Destroy();
  }

  [Then("the stockpile holds {int} {string}")]
  public void AssertStockpileHolds(PickleContext ctx, int expected, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    Zone_Stockpile? stockpile = map.zoneManager.AllZones.OfType<Zone_Stockpile>().FirstOrDefault();
    ctx.Require(stockpile != null, "the map has no stockpile zone");

    int actual = stockpile!.AllContainedThings.Where(t => t.def == def).Sum(t => t.stackCount);
    ctx.Assert(actual == expected, $"stockpile should hold {expected} {defName}; holds {actual}");
  }
}
