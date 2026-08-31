using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

/// <summary>Ideology, Royalty and Anomaly state. Tag the scenario `@requires:` the dlc.</summary>
[PickleSteps]
public class DlcSteps {
  [Then("the ideo has precept {string}")]
  public void AssertPrecept(PickleContext ctx, string preceptDefName) {
    Ideo ideo = RequireIdeo(ctx);

    ctx.Assert(
        ideo.PreceptsListForReading.Any(p => p.def.defName == preceptDefName),
        $"the player ideo should have precept '{preceptDefName}'; {DescribePrecepts(ideo)}");
  }

  [Then("the ideo has no precept {string}")]
  public void AssertNoPrecept(PickleContext ctx, string preceptDefName) {
    Ideo ideo = RequireIdeo(ctx);

    ctx.Assert(
        !ideo.PreceptsListForReading.Any(p => p.def.defName == preceptDefName),
        $"the player ideo should not have precept '{preceptDefName}'; {DescribePrecepts(ideo)}");
  }

  // No rewards and no letter: a granted title is scenario setup, not a story beat.
  [When("I give {string} the title {string}")]
  public void GiveTitle(PickleContext ctx, string nickname, string titleDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    RoyalTitleDef title = DefLookup.Require<RoyalTitleDef>(titleDefName);

    ctx.Require(pawn.royalty != null, $"pawn '{nickname}' has no royalty tracker");
    Faction? empire = Faction.OfEmpire;
    ctx.Require(empire != null, "no Empire faction exists on this world, so no title can be granted");

    pawn.royalty!.SetTitle(empire!, title, grantRewards: false, rewardsOnlyForNewestTitle: false, sendLetter: false);
  }

  [Then("{string} has title {string}")]
  public void AssertTitle(PickleContext ctx, string nickname, string titleDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    RoyalTitleDef title = DefLookup.Require<RoyalTitleDef>(titleDefName);

    ctx.Assert(
        pawn.royalty?.AllTitlesForReading.Any(t => t.def == title) == true,
        $"pawn '{nickname}' should hold the title '{titleDefName}'; {DescribeTitles(pawn)}");
  }

  [Then("{string} psylink level is {int}")]
  public void AssertPsylink(PickleContext ctx, string nickname, int expected) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    int actual = PawnUtility.GetPsylinkLevel(pawn);

    ctx.Assert(
        actual == expected,
        $"pawn '{nickname}' psylink level should be {expected}; it is {actual}");
  }

  [Then("the {string} at \\({int}, {int}\\) is studiable")]
  public void AssertStudiable(PickleContext ctx, string defName, int x, int z) {
    CompStudiable studiable = RequireStudiable(ctx, defName, x, z);
    bool ever = studiable.EverStudiable(out string reason);

    ctx.Assert(
        studiable.CurrentlyStudiable(),
        $"the {defName} at ({x}, {z}) should be studiable now; " +
        $"ever studiable={ever}{DescribeReason(reason)}, completed={studiable.Completed}");
  }

  [Then("the {string} at \\({int}, {int}\\) study knowledge is above {float}")]
  public void AssertStudyKnowledge(PickleContext ctx, string defName, int x, int z, float bound) {
    CompStudiable studiable = RequireStudiable(ctx, defName, x, z);
    float actual = studiable.anomalyKnowledgeGained;

    ctx.Assert(
        actual > bound,
        $"the {defName} at ({x}, {z}) study knowledge should be above {bound}; it is {actual}. " +
        $"completed={studiable.Completed}");
  }

  private static Ideo RequireIdeo(PickleContext ctx) {
    Ideo? ideo = Faction.OfPlayer?.ideos?.PrimaryIdeo;
    ctx.Require(
        ideo != null,
        "the player faction has no ideo. tag the scenario '@requires:Ideology' so a run " +
        "without the dlc skips it rather than failing here");

    return ideo!;
  }

  private static CompStudiable RequireStudiable(PickleContext ctx, string defName, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Thing thing = MapLookup.RequireThingAt(ctx, map, new IntVec3(x, 0, z), def);

    CompStudiable? studiable = thing.TryGetComp<CompStudiable>();
    ctx.Require(
        studiable != null,
        $"the {defName} at ({x}, {z}) cannot be studied; it carries no CompStudiable");

    return studiable!;
  }

  private static string DescribeReason(string reason) {
    return string.IsNullOrEmpty(reason) ? string.Empty : $" ({reason})";
  }

  private static string DescribePrecepts(Ideo ideo) {
    List<string> names = [.. ideo.PreceptsListForReading.Select(p => p.def.defName).OrderBy(n => n, StringComparer.Ordinal)];
    return names.Count == 0 ? "the ideo has no precepts" : $"precepts: {string.Join(", ", names)}";
  }

  private static string DescribeTitles(Pawn pawn) {
    List<string> names = [.. pawn.royalty?.AllTitlesForReading.Select(t => t.def.defName) ?? []];
    return names.Count == 0 ? "the pawn holds no titles" : $"titles: {string.Join(", ", names)}";
  }
}
