using System;
using System.Linq;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// Stat values on pawns and things. A StatPart or a StatWorker patch changes the number
/// without changing the def, so no other check in Pickle can see one.
/// </summary>
[PickleSteps]
public class StatSteps {
  [Then("{string} stat {string} is {float}")]
  public void AssertPawnStat(PickleContext ctx, string nickname, string statDefName, float expected) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    AssertNear(ctx, pawn, $"pawn '{nickname}'", statDefName, expected);
  }

  [Then("{string} stat {string} is above {float}")]
  public void AssertPawnStatAbove(PickleContext ctx, string nickname, string statDefName, float threshold) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    AssertCompared(ctx, pawn, $"pawn '{nickname}'", statDefName, actual => actual > threshold, $"above {threshold}");
  }

  [Then("{string} stat {string} is below {float}")]
  public void AssertPawnStatBelow(PickleContext ctx, string nickname, string statDefName, float threshold) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    AssertCompared(ctx, pawn, $"pawn '{nickname}'", statDefName, actual => actual < threshold, $"below {threshold}");
  }

  [Then("the {string} at \\({int}, {int}\\) stat {string} is {float}")]
  public void AssertThingStat(PickleContext ctx, string defName, int x, int z, string statDefName, float expected) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    Map map = MapLookup.RequireMap(ctx);
    Thing thing = MapLookup.RequireThingAt(ctx, map, new IntVec3(x, 0, z), def);

    AssertNear(ctx, thing, $"the {defName} at ({x}, {z})", statDefName, expected);
  }

  private static void AssertNear(
      PickleContext ctx, Thing thing, string subject, string statDefName, float expected) {
    StatDef stat = DefLookup.Require<StatDef>(statDefName);
    float actual = thing.GetStatValue(stat);
    float tolerance = StatTolerance.For(expected);
    bool passed = StatTolerance.IsNear(actual, expected);

    ctx.Assert(passed, passed ? null : Describe(ctx, thing, subject, stat, actual, $"{expected} within {tolerance:G3}"));
  }

  private static void AssertCompared(
      PickleContext ctx, Thing thing, string subject, string statDefName, Func<float, bool> test, string expectation) {
    StatDef stat = DefLookup.Require<StatDef>(statDefName);
    float actual = thing.GetStatValue(stat);
    bool passed = test(actual);

    ctx.Assert(passed, passed ? null : Describe(ctx, thing, subject, stat, actual, expectation));
  }

  private static string Describe(
      PickleContext ctx, Thing thing, string subject, StatDef stat, float actual, string expectation) {
    StatRequest request = StatRequest.For(thing);
    string explanation = Explain(request, stat, actual);
    ctx.Attach($"stat-{stat.defName}", explanation);

    // Without this note a stat that does not apply reads as a real measurement, when the
    // number is really the def default.
    string note = stat.Worker.ShouldShowFor(request)
        ? string.Empty
        : $" '{stat.defName}' does not apply to this thing, so the value is the def default.";

    return $"{subject} stat '{stat.defName}' should be {expectation}; actual {actual}.{note} " +
        $"breakdown: {Summarise(explanation)} (full breakdown attached)";
  }

  private static string Explain(StatRequest request, StatDef stat, float actual) {
    try {
      return stat.Worker.GetExplanationFull(request, stat.toStringNumberSense, actual);
    } catch (Exception ex) {
      // A throwing explanation would replace the real assertion message with a stack trace.
      return $"(the stat worker could not explain this value: {ex.Message})";
    }
  }

  private static string Summarise(string explanation) {
    string[] lines = [.. explanation
        .Split('\n')
        .Select(line => line.Trim())
        .Where(line => line.Length > 0)
        .Take(3)];

    return lines.Length == 0 ? "(none)" : string.Join(" / ", lines);
  }
}
