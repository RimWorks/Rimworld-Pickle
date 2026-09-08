using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Runtime;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Incidents, ticks, drafting, killing, and letters, the levers that drive a scenario forward.</summary>
[PickleSteps]
public class SimSteps {
  /// <summary>Fires an incident at the storyteller's default points for the current map.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="defName">The incident def to fire.</param>
  [When("incident {string} fires")]
  public void IncidentFires(PickleContext ctx, string defName) {
    ExecuteIncident(ctx, defName, points: null);
  }

  /// <summary>Fires an incident at an explicit point value for the current map.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="defName">The incident def to fire.</param>
  /// <param name="points">The points to fire the incident with.</param>
  [When("incident {string} fires with {int} points")]
  public void IncidentFiresWithPoints(PickleContext ctx, string defName, int points) {
    ExecuteIncident(ctx, defName, points);
  }

  /// <summary>
  /// Waits a number of ticks. In fast mode this drives the tick loop directly instead of
  /// waiting on real frames, sampling each tick's cost for the perf budget steps.
  /// </summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="ticks">The number of game ticks to wait.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I wait {int} ticks")]
  public async Task WaitTicks(PickleContext ctx, int ticks) {
    if (PickleRunMode.Current != PickleRunMode.Mode.Fast) {
      await ctx.WaitTicks(ticks);
      return;
    }

    int targetTick = Find.TickManager.TicksGame + ticks;
    while (Find.TickManager.TicksGame < targetTick) {
      for (int i = 0; i < 60 && Find.TickManager.TicksGame < targetTick; i++) {
        // GetTimestamp rather than a Stopwatch: ten thousand allocations inside the tick
        // loop would show up in the thing being measured.
        long start = Stopwatch.GetTimestamp();
        Find.TickManager.DoSingleTick();
        TickCostSampler.Record(Stopwatch.GetTimestamp() - start);
      }

      await ctx.WaitFrames(1);
    }
  }

  /// <summary>Drafts a pawn.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  [When("I draft {string}")]
  public void Draft(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.drafter != null, $"pawn '{nickname}' has no draft controller (not a colonist?)");
    pawn.drafter!.Drafted = true;
  }

  /// <summary>Undrafts a pawn.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  [When("I undraft {string}")]
  public void Undraft(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.drafter != null, $"pawn '{nickname}' has no draft controller (not a colonist?)");
    pawn.drafter!.Drafted = false;
  }

  /// <summary>Kills a pawn outright.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  [When("I kill {string}")]
  public void Kill(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    pawn.Kill(null);
  }

  /// <summary>Dumps every colonist's current job and state for a failure report.</summary>
  /// <returns>A description of every colonist on the current map, or a note that there is no map.</returns>
  // Fires on any failed scenario in this suite, not just the pawn steps, so a
  // failure anywhere still shows what every colonist was doing at the time.
  [PickleStateDump]
  public string ColonistState() {
    Map? map = Find.CurrentMap;
    return map == null ? "no current map" : PawnState.DescribeColonists(map);
  }

  /// <summary>Asserts a pawn is drafted. Waits first, since draft state can land a tick after the order.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [Then("{string} is drafted")]
  public async Task AssertDrafted(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    await ctx.AssertEventually(
        () => pawn.drafter != null && pawn.drafter.Drafted,
        () => $"pawn '{nickname}' should be drafted; actual state: {PawnState.Describe(pawn)}");
  }

  /// <summary>Asserts a pawn's current job matches a def name. Waits first, since a job is assigned on the next think cycle.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="jobDefName">The expected job def name.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [Then("{string} has job {string}")]
  public async Task AssertHasJob(PickleContext ctx, string nickname, string jobDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    await ctx.AssertEventually(
        () => string.Equals(pawn.CurJobDef?.defName ?? "(none)", jobDefName, StringComparison.OrdinalIgnoreCase),
        () => $"pawn '{nickname}' should have job '{jobDefName}'; actual state: {PawnState.Describe(pawn)}");
  }

  /// <summary>Asserts a pawn is dead. Looks the pawn up among the living or the dead, since a living-only lookup would miss it.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  [Then("{string} is dead")]
  public void AssertDead(PickleContext ctx, string nickname) {
    Map map = RequireMap(ctx);
    Pawn pawn = PawnLookup.RequireLivingOrDead(nickname, map);
    ctx.Assert(pawn.Dead, $"pawn '{nickname}' should be dead; actual state: {PawnState.Describe(pawn)}");
  }

  /// <summary>Asserts a letter whose label contains a substring has arrived.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="labelSubstring">The substring the letter's label should contain.</param>
  [Then("a letter {string} has arrived")]
  public void AssertLetterArrived(PickleContext ctx, string labelSubstring) {
    List<Letter> letters = Find.LetterStack.LettersListForReading;
    bool found = letters.Any(letter =>
        letter.Label.ToString().IndexOf(labelSubstring, StringComparison.OrdinalIgnoreCase) >= 0);

    ctx.Assert(
        found,
        $"no letter containing '{labelSubstring}' has arrived; current letters: {DescribeLetters(letters)}");
  }

  private static void ExecuteIncident(PickleContext ctx, string defName, int? points) {
    Map map = RequireMap(ctx);
    IncidentDef def = DefLookup.Require<IncidentDef>(defName);
    IncidentParms parms = StorytellerUtility.DefaultParmsNow(def.category, map);
    if (points.HasValue) {
      parms.points = points.Value;
    }

    // An incident draws its faction, strategy, arrival mode and spawn cell from the
    // shared Rand stream, so without this the raid differs by how many rolls the rest
    // of the run already consumed. Pop puts the game's own stream back.
    bool fired;
    Rand.PushState(Gen.HashCombineInt(ctx.ScenarioSeed, GenText.StableStringHash(defName)));
    try {
      fired = def.Worker.TryExecute(parms);
    } finally {
      Rand.PopState();
    }

    // The worker declines rather than throws when it cannot place the incident, and a
    // discarded false turns into a confusing "no letter arrived" a step later.
    ctx.Assert(
        fired,
        fired ? null : $"incident '{defName}' declined to fire with {parms.points} points; faction: {parms.faction?.Name ?? "(unresolved)"}, map: {map}");
  }

  private static string DescribeLetters(List<Letter> letters) {
    if (letters.Count == 0) {
      return "(none)";
    }

    return string.Join(", ", letters.Select(letter => letter.Label.ToString()));
  }

  private static Map RequireMap(PickleContext ctx) {
    Map? map = Find.CurrentMap;
    ctx.Require(map != null, "no current map is loaded; load a save first with 'the save ... is loaded'");
    return map!;
  }
}
