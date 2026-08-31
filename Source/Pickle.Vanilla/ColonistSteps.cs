using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

/// <summary>Shapes a generated colonist, so a scenario never depends on a random roll.</summary>
[PickleSteps]
public class ColonistSteps {
  [Given("{string} has backstory {string}")]
  public void SetAdulthood(PickleContext ctx, string nickname, string backstoryDefName) {
    Pawn pawn = RequireStoried(ctx, nickname);
    pawn.story.Adulthood = DefLookup.Require<BackstoryDef>(backstoryDefName);
    NotifyStoryChanged(pawn);
  }

  [Given("{string} has childhood {string}")]
  public void SetChildhood(PickleContext ctx, string nickname, string backstoryDefName) {
    Pawn pawn = RequireStoried(ctx, nickname);
    pawn.story.Childhood = DefLookup.Require<BackstoryDef>(backstoryDefName);
    NotifyStoryChanged(pawn);
  }

  [Given("I give {string} the trait {string}")]
  public void GiveTrait(PickleContext ctx, string nickname, string traitDefName) {
    GiveTraitAtDegree(ctx, nickname, traitDefName, 0);
  }

  [Given("I give {string} the trait {string} at degree {int}")]
  public void GiveTraitAtDegree(PickleContext ctx, string nickname, string traitDefName, int degree) {
    Pawn pawn = RequireStoried(ctx, nickname);
    TraitDef def = DefLookup.Require<TraitDef>(traitDefName);

    ctx.Require(
        def.degreeDatas.Any(d => d.degree == degree),
        $"'{traitDefName}' has no degree {degree}; it has {DescribeDegrees(def)}");

    if (pawn.story.traits.HasTrait(def, degree)) {
      return;
    }

    pawn.story.traits.GainTrait(new Trait(def, degree));
    NotifyStoryChanged(pawn);
  }

  [Given("I take the trait {string} from {string}")]
  public void RemoveTrait(PickleContext ctx, string traitDefName, string nickname) {
    Pawn pawn = RequireStoried(ctx, nickname);
    TraitDef def = DefLookup.Require<TraitDef>(traitDefName);

    Trait? held = pawn.story.traits.allTraits.FirstOrDefault(t => t.def == def);
    if (held == null) {
      return;
    }

    pawn.story.traits.RemoveTrait(held);
    NotifyStoryChanged(pawn);
  }

  [Given("{string} is {int} years old")]
  public void SetAge(PickleContext ctx, string nickname, int years) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.ageTracker != null, $"pawn '{nickname}' has no age tracker");
    ctx.Require(years >= 0, $"an age of {years} is not a real age");

    pawn.ageTracker!.AgeBiologicalTicks = (long)years * GenDate.TicksPerYear;
  }

  [Given("{string} gender is {word}")]
  public void SetGender(PickleContext ctx, string nickname, string gender) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);

    pawn.gender = gender.ToLowerInvariant() switch {
      "male" => Gender.Male,
      "female" => Gender.Female,
      _ => throw new ArgumentException($"unknown gender '{gender}'; supported: male, female"),
    };
  }

  [Given("{string} has {word} passion for {string}")]
  public void SetPassion(PickleContext ctx, string nickname, string passion, string skillDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.skills != null, $"pawn '{nickname}' has no skills");

    SkillDef skill = DefLookup.Require<SkillDef>(skillDefName);
    SkillRecord record = pawn.skills!.GetSkill(skill);

    ctx.Require(
        !record.TotallyDisabled,
        $"pawn '{nickname}' cannot use '{skillDefName}', so a passion means nothing");

    record.passion = passion.ToLowerInvariant() switch {
      "no" => Passion.None,
      "none" => Passion.None,
      "minor" => Passion.Minor,
      "major" => Passion.Major,
      _ => throw new ArgumentException($"unknown passion '{passion}'; supported: none, minor, major"),
    };
  }

  [Then("{string} can do {string}")]
  public void AssertCanDo(PickleContext ctx, string nickname, string workDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    WorkTypeDef work = DefLookup.Require<WorkTypeDef>(workDefName);

    ctx.Assert(
        !pawn.WorkTypeIsDisabled(work),
        $"pawn '{nickname}' cannot do '{workDefName}'. {DescribeStory(pawn)}");
  }

  [Then("{string} cannot do {string}")]
  public void AssertCannotDo(PickleContext ctx, string nickname, string workDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    WorkTypeDef work = DefLookup.Require<WorkTypeDef>(workDefName);

    ctx.Assert(
        pawn.WorkTypeIsDisabled(work),
        $"pawn '{nickname}' can do '{workDefName}'. {DescribeStory(pawn)}");
  }

  // Backstories and traits both feed the disabled work cache, so it has to be dropped
  // whenever either changes or the pawn keeps its old capabilities.
  private static void NotifyStoryChanged(Pawn pawn) {
    pawn.Notify_DisabledWorkTypesChanged();
    pawn.skills?.Notify_SkillDisablesChanged();
    pawn.workSettings?.Notify_DisabledWorkTypesChanged();
  }

  private static Pawn RequireStoried(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.story != null, $"pawn '{nickname}' has no story, so it has no backstory or traits");
    return pawn;
  }

  private static string DescribeDegrees(TraitDef def) {
    return string.Join(", ", def.degreeDatas.Select(d => d.degree.ToString()));
  }

  private static string DescribeStory(Pawn pawn) {
    string traits = pawn.story?.traits?.allTraits.Count > 0
        ? string.Join(", ", pawn.story.traits.allTraits.Select(t => t.def.defName))
        : "(none)";

    return $"childhood {pawn.story?.Childhood?.defName ?? "(none)"}, " +
        $"adulthood {pawn.story?.Adulthood?.defName ?? "(none)"}, traits {traits}";
  }
}
