using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;
using Verse.AI;

namespace Pickle.Vanilla;

/// <summary>
/// Health, needs, skills, traits, and what a pawn is carrying. Most mods change one
/// of these, and asserting on the current job alone cannot see any of it.
/// </summary>
[PickleSteps]
public class PawnSteps {
  [Then("{string} is downed")]
  public async Task AssertDowned(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    await ctx.AssertEventually(
        () => pawn.Downed,
        () => $"pawn '{nickname}' should be downed; actual state: {PawnState.Describe(pawn)}");
  }

  [Then("{string} is healthy")]
  public void AssertHealthy(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Assert(
        !pawn.Downed && !pawn.health.HasHediffsNeedingTend(),
        $"pawn '{nickname}' should be healthy; {DescribeHealth(pawn)}");
  }

  [Then("{string} has hediff {string}")]
  public void AssertHediff(PickleContext ctx, string nickname, string hediffDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    HediffDef def = DefLookup.Require<HediffDef>(hediffDefName);
    ctx.Assert(
        pawn.health.hediffSet.HasHediff(def),
        $"pawn '{nickname}' should have hediff '{hediffDefName}'; {DescribeHealth(pawn)}");
  }

  [Then("{string} health is above {int} percent")]
  public void AssertHealthAbove(PickleContext ctx, string nickname, int percent) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    float actual = pawn.health.summaryHealth.SummaryHealthPercent * 100f;
    ctx.Assert(
        actual > percent,
        $"pawn '{nickname}' health should be above {percent}%; actual {actual:F0}%");
  }

  [Then("{string} has no hediffs")]
  public void AssertNoHediffs(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Assert(
        pawn.health.hediffSet.hediffs.Count == 0,
        $"pawn '{nickname}' should carry no hediffs; {DescribeHealth(pawn)}");
  }

  [Then("{string} has no hediff {string}")]
  public void AssertNoHediff(PickleContext ctx, string nickname, string hediffDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    HediffDef def = DefLookup.Require<HediffDef>(hediffDefName);
    ctx.Assert(
        !pawn.health.hediffSet.HasHediff(def),
        $"pawn '{nickname}' should not have hediff '{hediffDefName}'; {DescribeHealth(pawn)}");
  }

  // A generated colonist arrives with whatever age and history rolled it, so a scenario
  // that wants a well pawn has to say so rather than hope for one.
  [When("I heal {string}")]
  public void Heal(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);

    foreach (Hediff hediff in pawn.health.hediffSet.hediffs.ToList()) {
      pawn.health.RemoveHediff(hediff);
    }
  }

  [When("{string} is cured of hediff {string}")]
  public void CureHediff(PickleContext ctx, string nickname, string hediffDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    HediffDef def = DefLookup.Require<HediffDef>(hediffDefName);

    foreach (Hediff hediff in pawn.health.hediffSet.hediffs.Where(h => h.def == def).ToList()) {
      pawn.health.RemoveHediff(hediff);
    }
  }

  [When("{string} is given hediff {string}")]
  public void GiveHediff(PickleContext ctx, string nickname, string hediffDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    pawn.health.AddHediff(DefLookup.Require<HediffDef>(hediffDefName));
  }

  [When("{string} attacks {string}")]
  public async Task Attack(PickleContext ctx, string attackerName, string targetName) {
    Pawn attacker = PawnLookup.RequireLiving(attackerName);
    Pawn target = PawnLookup.RequireLiving(targetName);

    ctx.Require(attacker.drafter != null, $"pawn '{attackerName}' cannot be drafted, so it cannot be ordered to attack");
    attacker.drafter!.Drafted = true;

    Job job = JobMaker.MakeJob(JobDefOf.AttackMelee, target);
    job.playerForced = true;
    attacker.jobs.TryTakeOrderedJob(job, JobTag.Misc);

    await ctx.WaitFrames(1);
  }

  [When("I order {string} to \\({int}, {int}\\)")]
  public async Task OrderTo(PickleContext ctx, string nickname, int x, int z) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Map map = pawn.Map;
    IntVec3 cell = new IntVec3(x, 0, z);

    ctx.Require(cell.InBounds(map), $"cell ({x}, {z}) is outside the map, which is {map.Size.x} by {map.Size.z}");
    ctx.Require(
        pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly),
        $"pawn '{nickname}' cannot reach ({x}, {z}) from {pawn.Position}. {DescribeCell(map, cell)}; " +
        $"pawn spawned={pawn.Spawned} standable={pawn.Position.Standable(map)}");

    Job job = JobMaker.MakeJob(JobDefOf.Goto, cell);
    job.playerForced = true;
    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

    await ctx.WaitFrames(1);
  }

  // A cell literal is a guess about a map you cannot see. The stockpile is somewhere a
  // colonist can always stand, so a scenario can order a walk without knowing the map.
  [When("I order {string} to the stockpile")]
  public async Task OrderToStockpile(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Map map = pawn.Map;

    Zone_Stockpile? stockpile = map.zoneManager.AllZones.OfType<Zone_Stockpile>().FirstOrDefault();
    ctx.Require(stockpile != null, "the map has no stockpile zone to walk to");

    IntVec3 target = stockpile!.Cells.FirstOrDefault(c => c.Standable(map) && pawn.CanReach(c, PathEndMode.OnCell, Danger.Deadly));
    ctx.Require(
        target.IsValid,
        $"pawn '{nickname}' cannot reach any cell of the stockpile from {pawn.Position}");

    Job job = JobMaker.MakeJob(JobDefOf.Goto, target);
    job.playerForced = true;
    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);

    await ctx.WaitFrames(1);
  }

  // Picks the furthest cell the pawn can actually reach, so a scenario gets a long walk
  // on any map without naming a coordinate that may be inside a mountain.
  [When("I order {string} to the far side of the map")]
  public async Task OrderFar(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Map map = pawn.Map;
    IntVec3 from = pawn.Position;

    IntVec3 best = IntVec3.Invalid;
    float bestDistance = -1f;
    foreach (IntVec3 cell in map.AllCells) {
      float distance = cell.DistanceToSquared(from);
      if (distance <= bestDistance || !cell.Standable(map)) {
        continue;
      }

      if (pawn.CanReach(cell, PathEndMode.OnCell, Danger.Deadly)) {
        best = cell;
        bestDistance = distance;
      }
    }

    ctx.Require(best.IsValid, $"pawn '{nickname}' cannot reach anywhere from {from}");

    Job job = JobMaker.MakeJob(JobDefOf.Goto, best);
    job.playerForced = true;
    pawn.jobs.TryTakeOrderedJob(job, JobTag.Misc);
    ctx.Set(best);

    await ctx.WaitFrames(1);
  }

  [Then("{string} needs {string} is below {int} percent")]
  public void AssertNeedBelow(PickleContext ctx, string nickname, string needDefName, int percent) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Need need = RequireNeed(pawn, needDefName);
    float actual = need.CurLevelPercentage * 100f;
    ctx.Assert(
        actual < percent,
        $"pawn '{nickname}' need '{needDefName}' should be below {percent}%; actual {actual:F0}%");
  }

  [When("{string} needs {string} is set to {int} percent")]
  public void SetNeed(PickleContext ctx, string nickname, string needDefName, int percent) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    RequireNeed(pawn, needDefName).CurLevelPercentage = percent / 100f;
  }

  [Then("{string} mood is above {int} percent")]
  public void AssertMoodAbove(PickleContext ctx, string nickname, int percent) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Need_Mood? mood = pawn.needs?.mood;
    ctx.Require(mood != null, $"pawn '{nickname}' has no mood need");
    float actual = mood!.CurLevelPercentage * 100f;
    ctx.Assert(actual > percent, $"pawn '{nickname}' mood should be above {percent}%; actual {actual:F0}%");
  }

  [Then("{string} has skill {string} at level {int}")]
  public void AssertSkillLevel(PickleContext ctx, string nickname, string skillDefName, int level) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    SkillRecord skill = RequireSkill(pawn, skillDefName);
    ctx.Assert(
        skill.Level == level,
        $"pawn '{nickname}' skill '{skillDefName}' should be level {level}; actual {skill.Level}. {DescribeSkill(skill)}");
  }

  // Level is levelInt plus Aptitude, and a disabled skill always reads 0, so setting the
  // raw value is not the level the author asked for.
  [When("{string} skill {string} is set to level {int}")]
  public void SetSkillLevel(PickleContext ctx, string nickname, string skillDefName, int level) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    SkillRecord skill = RequireSkill(pawn, skillDefName);

    ctx.Require(
        !skill.TotallyDisabled,
        $"pawn '{nickname}' cannot use '{skillDefName}', so its level is always 0. {DescribeSkill(skill)}");

    skill.Level = level - skill.Aptitude;
  }

  [Then("{string} has trait {string}")]
  public void AssertTrait(PickleContext ctx, string nickname, string traitDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    TraitDef def = DefLookup.Require<TraitDef>(traitDefName);
    ctx.Assert(
        pawn.story?.traits?.HasTrait(def) == true,
        $"pawn '{nickname}' should have trait '{traitDefName}'; traits: {DescribeTraits(pawn)}");
  }

  [Then("{string} is carrying {int} {string}")]
  public void AssertCarrying(PickleContext ctx, string nickname, int count, string defName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThingDef def = DefLookup.Require<ThingDef>(defName);
    int actual = CountHeld(pawn, def);
    ctx.Assert(
        actual == count,
        $"pawn '{nickname}' should carry {count} {defName}; actual {actual}. holding: {DescribeHeld(pawn)}");
  }

  [Then("{string} is carrying nothing")]
  public void AssertCarryingNothing(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Assert(
        pawn.carryTracker?.CarriedThing == null && (pawn.inventory?.innerContainer.Count ?? 0) == 0,
        $"pawn '{nickname}' should carry nothing; holding: {DescribeHeld(pawn)}");
  }

  private static Need RequireNeed(Pawn pawn, string needDefName) {
    NeedDef def = DefLookup.Require<NeedDef>(needDefName);
    Need? need = pawn.needs?.TryGetNeed(def);
    if (need == null) {
      string present = string.Join(", ", pawn.needs?.AllNeeds.Select(n => n.def.defName) ?? []);
      throw new InvalidOperationException($"pawn '{pawn.LabelShort}' has no need '{needDefName}'. needs: {present}");
    }

    return need;
  }

  private static SkillRecord RequireSkill(Pawn pawn, string skillDefName) {
    SkillDef def = DefLookup.Require<SkillDef>(skillDefName);
    SkillRecord? skill = pawn.skills?.GetSkill(def);
    if (skill == null) {
      throw new InvalidOperationException($"pawn '{pawn.LabelShort}' has no skills tracker");
    }

    return skill;
  }

  private static int CountHeld(Pawn pawn, ThingDef def) {
    int carried = pawn.carryTracker?.CarriedThing?.def == def ? pawn.carryTracker.CarriedThing!.stackCount : 0;
    int inInventory = pawn.inventory?.innerContainer
        .Where(t => t.def == def)
        .Sum(t => t.stackCount) ?? 0;

    return carried + inInventory;
  }

  private static string DescribeHeld(Pawn pawn) {
    List<string> held = [];
    if (pawn.carryTracker?.CarriedThing != null) {
      held.Add($"{pawn.carryTracker.CarriedThing.stackCount}x {pawn.carryTracker.CarriedThing.def.defName} (carried)");
    }

    foreach (Thing thing in pawn.inventory?.innerContainer ?? Enumerable.Empty<Thing>()) {
      held.Add($"{thing.stackCount}x {thing.def.defName}");
    }

    return held.Count == 0 ? "(nothing)" : string.Join(", ", held);
  }

  private static string DescribeCell(Map map, IntVec3 cell) {
    TerrainDef? terrain = cell.GetTerrain(map);
    string things = string.Join(", ", cell.GetThingList(map).Select(t => t.def.defName));
    return $"target terrain={terrain?.defName ?? "?"} standable={cell.Standable(map)} " +
        $"walkable={cell.Walkable(map)} holds=[{things}]";
  }

  private static string DescribeSkill(SkillRecord skill) {
    if (skill.TotallyDisabled) {
      return "the skill is disabled for this pawn, so Level always reads 0";
    }

    return $"levelInt={skill.levelInt} aptitude={skill.Aptitude} passion={skill.passion}";
  }

  private static string DescribeTraits(Pawn pawn) {
    string[] traits = [.. pawn.story?.traits?.allTraits.Select(t => t.def.defName) ?? []];
    return traits.Length == 0 ? "(none)" : string.Join(", ", traits);
  }

  private static string DescribeHealth(Pawn pawn) {
    return $"downed={pawn.Downed} health={pawn.health.summaryHealth.SummaryHealthPercent:P0} " +
        $"hediffs={string.Join(", ", pawn.health.hediffSet.hediffs.Select(h => h.def.defName).Take(6))}";
  }
}
