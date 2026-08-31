using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

/// <summary>Which body part a hediff sits on, and the surgeries that put it there.</summary>
[PickleSteps]
public class SurgerySteps {
  [Then("{string} is missing {string}")]
  public void AssertMissing(PickleContext ctx, string nickname, string partLabel) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    BodyPartRecord part = BodyPartLookup.Require(pawn, partLabel);

    ctx.Assert(
        pawn.health.hediffSet.PartIsMissing(part),
        $"pawn '{nickname}' should be missing '{partLabel}'; {BodyPartLookup.Describe(pawn)}");
  }

  [Then("{string} is not missing {string}")]
  public void AssertNotMissing(PickleContext ctx, string nickname, string partLabel) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    BodyPartRecord part = BodyPartLookup.Require(pawn, partLabel);

    ctx.Assert(
        !pawn.health.hediffSet.PartIsMissing(part),
        $"pawn '{nickname}' should still have '{partLabel}'; {BodyPartLookup.Describe(pawn)}");
  }

  [Then("{string} has hediff {string} on {string}")]
  public void AssertHediffOnPart(PickleContext ctx, string nickname, string hediffDefName, string partLabel) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    HediffDef def = DefLookup.Require<HediffDef>(hediffDefName);
    BodyPartRecord part = BodyPartLookup.Require(pawn, partLabel);

    ctx.Assert(
        pawn.health.hediffSet.HasHediff(def, part, mustBeVisible: false),
        $"pawn '{nickname}' should have '{hediffDefName}' on '{partLabel}'; {DescribePlacedHediffs(pawn)}");
  }

  [Then("{string} has no hediff {string} on {string}")]
  public void AssertNoHediffOnPart(PickleContext ctx, string nickname, string hediffDefName, string partLabel) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    HediffDef def = DefLookup.Require<HediffDef>(hediffDefName);
    BodyPartRecord part = BodyPartLookup.Require(pawn, partLabel);

    ctx.Assert(
        !pawn.health.hediffSet.HasHediff(def, part, mustBeVisible: false),
        $"pawn '{nickname}' should have no '{hediffDefName}' on '{partLabel}'; {DescribePlacedHediffs(pawn)}");
  }

  [Then("{string} has {int} surgeries queued")]
  public void AssertSurgeryCount(PickleContext ctx, string nickname, int expected) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    int actual = pawn.health.surgeryBills?.Count ?? 0;

    ctx.Assert(
        actual == expected,
        $"pawn '{nickname}' should have {expected} surgeries queued; has {actual}. {DescribeSurgeries(pawn)}");
  }

  [When("{string} is given hediff {string} on {string}")]
  public async Task GiveHediffOnPart(PickleContext ctx, string nickname, string hediffDefName, string partLabel) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    HediffDef def = DefLookup.Require<HediffDef>(hediffDefName);
    BodyPartRecord part = BodyPartLookup.Require(pawn, partLabel);

    pawn.health.AddHediff(def, part);

    await ctx.AssertEventually(
        () => pawn.health.hediffSet.HasHediff(def, part, mustBeVisible: false),
        () => $"'{hediffDefName}' never landed on '{partLabel}' of '{nickname}'; {DescribePlacedHediffs(pawn)}");
  }

  // The damage Recipe_RemoveBodyPart.DamagePart deals, so cutting a vital part kills the pawn
  // exactly as the real surgery would.
  [When("I amputate {string} from {string}")]
  public async Task Amputate(PickleContext ctx, string partLabel, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    BodyPartRecord part = BodyPartLookup.Require(pawn, partLabel);

    ctx.Require(
        !pawn.health.hediffSet.PartIsMissing(part),
        $"pawn '{nickname}' has already lost '{partLabel}'; {BodyPartLookup.Describe(pawn)}");

    pawn.TakeDamage(new DamageInfo(DamageDefOf.SurgicalCut, 99999f, 999f, -1f, null, part));

    await ctx.AssertEventually(
        () => pawn.health.hediffSet.PartIsMissing(part),
        () => $"'{partLabel}' survived the cut on '{nickname}'; dead={pawn.Dead}. {BodyPartLookup.Describe(pawn)}");
  }

  // A null billDoer takes the recipe's own no-doctor path, which restores the part and adds
  // the hediff. No bed, no medicine, no doctor.
  [When("I install {string} on {string} of {string}")]
  public async Task Install(PickleContext ctx, string recipeDefName, string partLabel, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    RecipeDef recipe = DefLookup.Require<RecipeDef>(recipeDefName);
    BodyPartRecord part = RequireTargetablePart(ctx, pawn, recipe, partLabel);

    recipe.Worker.ApplyOnPawn(pawn, part, null, [], null);

    await ctx.AssertEventually(
        () => recipe.addsHediff == null || pawn.health.hediffSet.HasHediff(recipe.addsHediff, part, mustBeVisible: false),
        () => $"recipe '{recipeDefName}' added nothing to '{partLabel}' of '{nickname}'; {DescribePlacedHediffs(pawn)}");
  }

  [When("I queue surgery {string} on {string} of {string}")]
  public void QueueSurgery(PickleContext ctx, string recipeDefName, string partLabel, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    RecipeDef recipe = DefLookup.Require<RecipeDef>(recipeDefName);
    BodyPartRecord part = RequireTargetablePart(ctx, pawn, recipe, partLabel);

    HealthCardUtility.CreateSurgeryBill(pawn, recipe, part, null, sendMessages: false);
  }

  // A surgery has no finished event, so this watches the bill leave the queue. The game drops
  // a bill it decides is impossible, so assert the result afterwards too.
  [When("I wait for surgery {string} on {string} to finish", TimeoutSeconds = 185f)]
  public async Task WaitForSurgery(PickleContext ctx, string recipeDefName, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    RecipeDef recipe = DefLookup.Require<RecipeDef>(recipeDefName);

    ctx.Require(
        HasQueued(pawn, recipe),
        $"pawn '{nickname}' has no queued '{recipeDefName}'. {DescribeSurgeries(pawn)}");

    await ctx.AssertEventually(
        () => !HasQueued(pawn, recipe),
        () => $"surgery '{recipeDefName}' never left the queue of '{nickname}'. {DescribeSurgeries(pawn)}; " +
            $"{DescribeDoctors(pawn)}",
        180f);
  }

  private static BodyPartRecord RequireTargetablePart(
      PickleContext ctx, Pawn pawn, RecipeDef recipe, string partLabel) {
    BodyPartRecord part = BodyPartLookup.Require(pawn, partLabel);

    ctx.Require(recipe.Worker != null, $"recipe '{recipe.defName}' has no worker, so it operates on nothing");

    List<BodyPartRecord> targets = [.. recipe.Worker!.GetPartsToApplyOn(pawn, recipe)];
    ctx.Require(
        targets.Contains(part),
        $"recipe '{recipe.defName}' cannot be applied to '{partLabel}' of '{pawn.LabelShort}'; " +
        $"it can target {DescribeParts(targets)}");

    return part;
  }

  private static bool HasQueued(Pawn pawn, RecipeDef recipe) {
    return pawn.health.surgeryBills?.Bills.Any(b => b.recipe == recipe) == true;
  }

  private static string DescribeParts(List<BodyPartRecord> parts) {
    List<string> labels = [.. parts.Select(p => p.Label).Distinct().Take(6)];
    return labels.Count == 0 ? "nothing on this pawn" : string.Join(", ", labels);
  }

  private static string DescribePlacedHediffs(Pawn pawn) {
    List<string> placed = [.. pawn.health.hediffSet.hediffs
        .Select(h => $"{h.def.defName} on {h.Part?.Label ?? "the whole body"}")
        .Take(8)];

    return placed.Count == 0 ? "the pawn has no hediffs" : $"hediffs: {string.Join(", ", placed)}";
  }

  private static string DescribeSurgeries(Pawn pawn) {
    List<string> queued = [.. pawn.health.surgeryBills?.Bills
        .Select(b => $"{b.recipe?.defName ?? "(no recipe)"} on {(b as Bill_Medical)?.Part?.Label ?? "(no part)"}")
        ?? []];

    return queued.Count == 0 ? "queued surgeries: (none)" : $"queued surgeries: {string.Join(", ", queued)}";
  }

  // A queued surgery that never starts is almost always a missing doctor or a missing medical
  // bed, and neither shows up in the bill stack.
  private static string DescribeDoctors(Pawn pawn) {
    bool doctor = WorkGiver_PatientGoToBedTreatment.AnyAvailableDoctorFor(pawn);
    bool inBed = pawn.InBed();
    bool medicalBed = pawn.Map?.listerBuildings.allBuildingsColonist
        .Any(b => b is Building_Bed bed && bed.Medical) == true;

    return $"a doctor is available={doctor}, the patient is in bed={inBed}, " +
        $"the map has a medical bed={medicalBed}";
  }
}
