using System;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

/// <summary>
/// Waits for a condition instead of a tick count. A fixed wait is a guess about how
/// long the game needs, and it fails on a faster machine.
/// </summary>
[PickleSteps]
public class WaitSteps {
  private const float DefaultTimeoutSeconds = 30f;

  [When("I wait for letter {string}", TimeoutSeconds = 35f)]
  public async Task WaitForLetter(PickleContext ctx, string labelSubstring) {
    await WaitFor(
        ctx,
        () => Find.LetterStack.LettersListForReading
            .Any(l => l.Label.RawText?.IndexOf(labelSubstring, StringComparison.OrdinalIgnoreCase) >= 0),
        () => $"no letter matching '{labelSubstring}' arrived. letters: {DescribeLetters()}");
  }

  [When("I wait for {string} to have job {string}", TimeoutSeconds = 35f)]
  public async Task WaitForJob(PickleContext ctx, string nickname, string jobDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    await WaitFor(
        ctx,
        () => string.Equals(pawn.CurJobDef?.defName, jobDefName, StringComparison.OrdinalIgnoreCase),
        () => $"pawn '{nickname}' never took job '{jobDefName}'; actual state: {PawnState.Describe(pawn)}");
  }

  [When("I wait for a {string} to exist", TimeoutSeconds = 35f)]
  public async Task WaitForThing(PickleContext ctx, string defName) {
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    await WaitFor(
        ctx,
        () => Find.CurrentMap?.listerThings.ThingsOfDef(def).Count > 0,
        () => $"no {defName} appeared on the map");
  }

  [When("I wait for research {string} to finish", TimeoutSeconds = 35f)]
  public async Task WaitForResearch(PickleContext ctx, string defName) {
    ResearchProjectDef project = DefLookup.Require<ResearchProjectDef>(defName);
    await WaitFor(
        ctx,
        () => project.IsFinished,
        () => $"research '{defName}' did not finish; progress {project.ProgressPercent:P0}");
  }

  [When("I wait until {string} reaches the stockpile", TimeoutSeconds = 35f)]
  public async Task WaitForStockpile(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Map map = pawn.Map;

    await WaitFor(
        ctx,
        () => map.zoneManager.ZoneAt(pawn.Position) is Zone_Stockpile,
        () => $"pawn '{nickname}' never reached the stockpile; standing at {pawn.Position} " +
            $"doing {pawn.CurJobDef?.defName ?? "nothing"}");
  }

  // A tick count is a guess about how far the pawn has to walk. This ends when it
  // actually stops, so a film runs exactly as long as the journey.
  [When("I wait until {string} stops moving", TimeoutSeconds = 90f)]
  public async Task WaitUntilStill(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);

    // one frame first, or a pather that has not started yet reads as already stopped
    await ctx.WaitFrames(2);
    await ctx.AssertEventually(
        () => pawn.pather?.MovingNow != true,
        () => $"pawn '{nickname}' never stopped; at {pawn.Position} doing {pawn.CurJobDef?.defName ?? "nothing"}",
        85f);
  }

  [When("I wait until {string} is drafted", TimeoutSeconds = 35f)]
  public async Task WaitForDrafted(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    await WaitFor(
        ctx,
        () => pawn.drafter?.Drafted == true,
        () => $"pawn '{nickname}' never drafted; actual state: {PawnState.Describe(pawn)}");
  }

  private static async Task WaitFor(PickleContext ctx, Func<bool> condition, Func<string> describeFailure) {
    await ctx.AssertEventually(condition, describeFailure, DefaultTimeoutSeconds);
  }

  private static string DescribeLetters() {
    string[] labels = [.. Find.LetterStack.LettersListForReading.Select(l => l.Label.RawText ?? "(no label)")];
    return labels.Length == 0 ? "(none)" : string.Join(", ", labels);
  }
}
