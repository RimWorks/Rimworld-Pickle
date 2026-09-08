using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// What is on the map and where. Covers spawn and despawn testing, which asserting
/// on pawn state cannot reach.
/// </summary>
[PickleSteps]
public class MapSteps {
  /// <summary>Waits for at least one thing of the def to be on the map, so a spawn that takes a tick still passes.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The thing def to look for.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [Then("a {string} exists")]
  public async Task AssertThingExists(PickleContext ctx, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    await ctx.AssertEventually(
        () => map.listerThings.ThingsOfDef(def).Count > 0,
        () => $"expected at least one {defName} on the map; found none");
  }

  /// <summary>Checks nothing of the def is on the map. Reads the count now rather than waiting.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The thing def that should be absent.</param>
  [Then("no {string} exists")]
  public void AssertThingAbsent(PickleContext ctx, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    int count = map.listerThings.ThingsOfDef(def).Count;
    ctx.Assert(count == 0, $"expected no {defName} on the map; found {count}");
  }

  /// <summary>Checks the total stack count of a def across the whole map.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="expected">The stack count the map should hold.</param>
  /// <param name="defName">The thing def to count.</param>
  [Then("{int} {string} exist")]
  public void AssertThingCount(PickleContext ctx, int expected, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    int actual = map.listerThings.ThingsOfDef(def).Sum(t => t.stackCount);
    ctx.Assert(actual == expected, $"expected {expected} {defName}; found {actual}");
  }

  /// <summary>Checks a cell holds a thing of the def. The failure lists everything the cell really holds.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The thing def to look for.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
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

  /// <summary>Checks a cell holds nothing of the def.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The thing def that should be absent.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
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

  /// <summary>Checks a cell holds nothing at all.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  [Then("cell \\({int}, {int}\\) is empty")]
  public void AssertCellEmpty(PickleContext ctx, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    List<Thing> things = cell.GetThingList(map);
    ctx.Assert(things.Count == 0, $"cell ({x}, {z}) should be empty; holds: {MapLookup.DescribeCell(map, cell)}");
  }

  /// <summary>Spawns one thing of the def, made from its default stuff when the def needs stuff.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The thing def to spawn.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  [When("I spawn a {string} at \\({int}, {int}\\)")]
  public void SpawnAtCell(PickleContext ctx, string defName, int x, int z) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    Thing thing = ThingMaker.MakeThing(def, def.MadeFromStuff ? GenStuff.DefaultStuffFor(def) : null);
    GenSpawn.Spawn(thing, cell, map);
  }

  /// <summary>Destroys the thing of the def in that cell, failing when the cell holds none.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="defName">The thing def to destroy.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  [When("I destroy the {string} at \\({int}, {int}\\)")]
  public void DestroyAtCell(PickleContext ctx, string defName, int x, int z) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    MapLookup.RequireThingAt(ctx, map, new IntVec3(x, 0, z), def).Destroy();
  }

  /// <summary>Checks the first stockpile zone on the map holds a stack count of the def.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="expected">The stack count the stockpile should hold.</param>
  /// <param name="defName">The thing def to count.</param>
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
