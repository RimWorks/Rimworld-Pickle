using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI.Group;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Ideology, Royalty and Anomaly state. Tag the scenario `@requires:` the dlc.</summary>
[PickleSteps]
public class DlcSteps {
  /// <summary>Asserts the player's ideo carries a precept.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="preceptDefName">The precept def to look for.</param>
  [Then("the ideo has precept {string}")]
  public void AssertPrecept(PickleContext ctx, string preceptDefName) {
    Ideo ideo = RequireIdeo(ctx);

    ctx.Assert(
        ideo.PreceptsListForReading.Any(p => p.def.defName == preceptDefName),
        $"the player ideo should have precept '{preceptDefName}'; {DescribePrecepts(ideo)}");
  }

  /// <summary>Asserts the player's ideo does not carry a precept.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="preceptDefName">The precept def that should be absent.</param>
  [Then("the ideo has no precept {string}")]
  public void AssertNoPrecept(PickleContext ctx, string preceptDefName) {
    Ideo ideo = RequireIdeo(ctx);

    ctx.Assert(
        !ideo.PreceptsListForReading.Any(p => p.def.defName == preceptDefName),
        $"the player ideo should not have precept '{preceptDefName}'; {DescribePrecepts(ideo)}");
  }

  /// <summary>Starts a ritual precept directly, without going through its dialog.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="preceptDefName">The ritual precept def to start.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // The ritual dialog builds the same three things: a target, an organizer and role
  // assignments. This skips the dialog, not the ritual.
  [When("I start ritual {string}")]
  public async Task StartRitual(PickleContext ctx, string preceptDefName) {
    Precept_Ritual ritual = RequireRitual(ctx, preceptDefName);
    Map map = MapLookup.RequireMap(ctx);

    Pawn? organizer = map.mapPawns.FreeColonistsSpawned.FirstOrDefault();
    ctx.Require(organizer != null, "no spawned colonist can organise a ritual");

    ctx.Require(
        ritual.targetFilter != null && ritual.behavior != null,
        $"ritual '{preceptDefName}' cannot be started directly; it has no target filter or no " +
        $"behavior worker. {DescribeRituals(RequireIdeo(ctx))}");

    TargetInfo target = ritual.targetFilter!.BestTarget(new TargetInfo(organizer!), TargetInfo.Invalid);
    ctx.Require(
        target.IsValid,
        $"ritual '{preceptDefName}' has no valid target on this map. it usually wants a " +
        "ritual spot or an ideo building");

    ctx.Require(
        ritual.targetFilter.CanStart(new TargetInfo(organizer!), target, out string reason),
        $"ritual '{preceptDefName}' cannot start: {(reason.NullOrEmpty() ? "no reason given" : reason)}");

    string? blocker = ritual.behavior!.CanStartRitualNow(target, ritual);
    ctx.Require(blocker.NullOrEmpty(), $"ritual '{preceptDefName}' cannot start now: {blocker}");

    // A bare new RitualRoleAssignments never gets Setup called, and FillPawns NREs on the
    // null pawn lists. The dialog's own factory is the only path that wires it correctly.
    RitualRoleAssignments assignments = Dialog_BeginRitual.CreateRitualRoleAssignments(
        ritual, target, map, filter: null, requiredPawns: null, forcedForRole: null, selectedPawn: null);
    assignments.FillPawns(null, target);
    ritual.behavior.TryExecuteOn(target, organizer!, ritual, null, assignments, playerForced: true);

    await ctx.AssertEventually(
        () => RitualRunning(map, ritual),
        () => $"ritual '{preceptDefName}' never started; {DescribeLords(map)}");
  }

  /// <summary>Asserts a ritual is currently running.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="preceptDefName">The ritual precept def to check.</param>
  [Then("a ritual {string} is running")]
  public void AssertRitualRunning(PickleContext ctx, string preceptDefName) {
    Precept_Ritual ritual = RequireRitual(ctx, preceptDefName);
    Map map = MapLookup.RequireMap(ctx);

    ctx.Assert(
        RitualRunning(map, ritual),
        $"ritual '{preceptDefName}' should be running; {DescribeLords(map)}");
  }

  /// <summary>Despawns an entity and places it inside a holding platform.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="kindDefName">The pawn kind def of the entity to contain.</param>
  /// <param name="x">The platform's cell x coordinate.</param>
  /// <param name="z">The platform's cell z coordinate.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I contain {string} on the platform at \\({int}, {int}\\)")]
  public async Task Contain(PickleContext ctx, string kindDefName, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    PawnKindDef kind = DefLookup.Require<PawnKindDef>(kindDefName);
    Building_HoldingPlatform platform = RequirePlatform(ctx, map, x, z);

    Pawn? entity = map.mapPawns.AllPawnsSpawned.FirstOrDefault(p => p.kindDef == kind);
    ctx.Require(
        entity != null,
        $"no spawned '{kindDefName}' to contain. spawn one first with 'I spawn a ... pawn at'");
    ctx.Require(!platform.Occupied, $"the platform at ({x}, {z}) already holds {platform.HeldPawn?.LabelShort}");

    if (entity!.Spawned) {
      entity.DeSpawn();
    }

    platform.innerContainer.TryAdd(entity, canMergeWithExistingStacks: false);

    await ctx.AssertEventually(
        () => platform.HeldPawn?.kindDef == kind,
        () => $"'{kindDefName}' never landed on the platform at ({x}, {z}); " +
            $"it holds {platform.HeldPawn?.LabelShort ?? "nothing"}");
  }

  /// <summary>Asserts a holding platform holds an entity of the given kind.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="x">The platform's cell x coordinate.</param>
  /// <param name="z">The platform's cell z coordinate.</param>
  /// <param name="kindDefName">The expected pawn kind def.</param>
  [Then("the platform at \\({int}, {int}\\) holds {string}")]
  public void AssertPlatformHolds(PickleContext ctx, int x, int z, string kindDefName) {
    Map map = MapLookup.RequireMap(ctx);
    PawnKindDef kind = DefLookup.Require<PawnKindDef>(kindDefName);
    Building_HoldingPlatform platform = RequirePlatform(ctx, map, x, z);

    ctx.Assert(
        platform.HeldPawn?.kindDef == kind,
        $"the platform at ({x}, {z}) should hold '{kindDefName}'; " +
        $"it holds {platform.HeldPawn?.LabelShort ?? "nothing"}");
  }

  /// <summary>Asserts a holding platform holds nothing.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="x">The platform's cell x coordinate.</param>
  /// <param name="z">The platform's cell z coordinate.</param>
  [Then("the platform at \\({int}, {int}\\) is empty")]
  public void AssertPlatformEmpty(PickleContext ctx, int x, int z) {
    Map map = MapLookup.RequireMap(ctx);
    Building_HoldingPlatform platform = RequirePlatform(ctx, map, x, z);

    ctx.Assert(
        !platform.Occupied,
        $"the platform at ({x}, {z}) should be empty; it holds {platform.HeldPawn?.LabelShort}");
  }

  /// <summary>Grants a pawn a royal title, skipping rewards and the letter.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="titleDefName">The royal title def to grant.</param>
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

  /// <summary>Asserts a pawn holds a royal title.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="titleDefName">The royal title def expected.</param>
  [Then("{string} has title {string}")]
  public void AssertTitle(PickleContext ctx, string nickname, string titleDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    RoyalTitleDef title = DefLookup.Require<RoyalTitleDef>(titleDefName);

    ctx.Assert(
        pawn.royalty?.AllTitlesForReading.Any(t => t.def == title) == true,
        $"pawn '{nickname}' should hold the title '{titleDefName}'; {DescribeTitles(pawn)}");
  }

  /// <summary>Asserts a pawn's psylink level is exactly a value.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="expected">The expected psylink level.</param>
  [Then("{string} psylink level is {int}")]
  public void AssertPsylink(PickleContext ctx, string nickname, int expected) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    int actual = PawnUtility.GetPsylinkLevel(pawn);

    ctx.Assert(
        actual == expected,
        $"pawn '{nickname}' psylink level should be {expected}; it is {actual}");
  }

  /// <summary>Asserts an anomaly entity is currently studiable.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="defName">The thing def to check.</param>
  /// <param name="x">The thing's cell x coordinate.</param>
  /// <param name="z">The thing's cell z coordinate.</param>
  [Then("the {string} at \\({int}, {int}\\) is studiable")]
  public void AssertStudiable(PickleContext ctx, string defName, int x, int z) {
    CompStudiable studiable = RequireStudiable(ctx, defName, x, z);
    bool ever = studiable.EverStudiable(out string reason);

    ctx.Assert(
        studiable.CurrentlyStudiable(),
        $"the {defName} at ({x}, {z}) should be studiable now; " +
        $"ever studiable={ever}{DescribeReason(reason)}, completed={studiable.Completed}");
  }

  /// <summary>Asserts the anomaly study knowledge gained from an entity is above a bound.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="defName">The thing def to check.</param>
  /// <param name="x">The thing's cell x coordinate.</param>
  /// <param name="z">The thing's cell z coordinate.</param>
  /// <param name="bound">The lower bound, exclusive.</param>
  [Then("the {string} at \\({int}, {int}\\) study knowledge is above {float}")]
  public void AssertStudyKnowledge(PickleContext ctx, string defName, int x, int z, float bound) {
    CompStudiable studiable = RequireStudiable(ctx, defName, x, z);
    float actual = studiable.anomalyKnowledgeGained;

    ctx.Assert(
        actual > bound,
        $"the {defName} at ({x}, {z}) study knowledge should be above {bound}; it is {actual}. " +
        $"completed={studiable.Completed}");
  }

  private static Precept_Ritual RequireRitual(PickleContext ctx, string preceptDefName) {
    Ideo ideo = RequireIdeo(ctx);
    Precept_Ritual? ritual = ideo.PreceptsListForReading
        .OfType<Precept_Ritual>()
        .FirstOrDefault(r => r.def.defName == preceptDefName);

    ctx.Require(ritual != null, $"the player ideo has no ritual '{preceptDefName}'; {DescribeRituals(ideo)}");
    return ritual!;
  }

  private static Building_HoldingPlatform RequirePlatform(PickleContext ctx, Map map, int x, int z) {
    IntVec3 cell = new IntVec3(x, 0, z);
    MapLookup.RequireInBounds(ctx, map, cell);

    Building_HoldingPlatform? platform = cell.GetThingList(map).OfType<Building_HoldingPlatform>().FirstOrDefault();
    ctx.Require(
        platform != null,
        $"no holding platform at ({x}, {z}); the cell holds {MapLookup.DescribeCell(map, cell)}");

    return platform!;
  }

  private static bool RitualRunning(Map map, Precept_Ritual ritual) {
    return map.lordManager.lords.Any(l => l.LordJob is LordJob_Ritual job && job.Ritual == ritual);
  }

  // A generated ideo carries rituals with no target filter, and those cannot be started from
  // a step at all, so the list says which ones can.
  private static string DescribeRituals(Ideo ideo) {
    List<string> names = [.. ideo.PreceptsListForReading
        .OfType<Precept_Ritual>()
        .Select(r => $"{r.def.defName}{(r.targetFilter != null && r.behavior != null ? " (startable)" : string.Empty)}")];

    return names.Count == 0 ? "the ideo has no rituals" : $"rituals: {string.Join(", ", names)}";
  }

  private static string DescribeLords(Map map) {
    List<string> jobs = [.. map.lordManager.lords
        .Select(l => l.LordJob is LordJob_Ritual r ? r.Ritual?.def.defName ?? "(ritual)" : l.LordJob.GetType().Name)];

    return jobs.Count == 0 ? "no lord is running on this map" : $"lords: {string.Join(", ", jobs)}";
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
