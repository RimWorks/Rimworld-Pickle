using System;
using System.Collections.Generic;
using System.Linq;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Shapes a generated colonist, so a scenario never depends on a random roll.</summary>
[PickleSteps]
public class ColonistSteps {
  /// <summary>Sets a pawn's adult backstory.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to change.</param>
  /// <param name="backstoryDefName">The backstory to set.</param>
  [Given("{string} has backstory {string}")]
  public void SetAdulthood(PickleContext ctx, string nickname, string backstoryDefName) {
    Pawn pawn = RequireStoried(ctx, nickname);
    pawn.story.Adulthood = DefLookup.Require<BackstoryDef>(backstoryDefName);
    NotifyStoryChanged(pawn);
  }

  /// <summary>Sets a pawn's childhood backstory.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to change.</param>
  /// <param name="backstoryDefName">The backstory to set.</param>
  [Given("{string} has childhood {string}")]
  public void SetChildhood(PickleContext ctx, string nickname, string backstoryDefName) {
    Pawn pawn = RequireStoried(ctx, nickname);
    pawn.story.Childhood = DefLookup.Require<BackstoryDef>(backstoryDefName);
    NotifyStoryChanged(pawn);
  }

  /// <summary>Gives a pawn a trait at its default degree, <c>0</c>. Does nothing if the pawn already has it.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to give the trait to.</param>
  /// <param name="traitDefName">The trait to give.</param>
  [Given("I give {string} the trait {string}")]
  public void GiveTrait(PickleContext ctx, string nickname, string traitDefName) {
    GiveTraitAtDegree(ctx, nickname, traitDefName, 0);
  }

  /// <summary>Gives a pawn a trait at a specific degree. Does nothing if the pawn already has it at that degree.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to give the trait to.</param>
  /// <param name="traitDefName">The trait to give.</param>
  /// <param name="degree">The degree to give it at, which must be one the trait defines.</param>
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

  /// <summary>Removes a trait from a pawn, if it has one. Does nothing otherwise.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="traitDefName">The trait to remove.</param>
  /// <param name="nickname">The pawn to remove it from.</param>
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

  /// <summary>Sets a pawn's biological age in whole years.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to age.</param>
  /// <param name="years">The age in years, which must not be negative.</param>
  [Given("{string} is {int} years old")]
  public void SetAge(PickleContext ctx, string nickname, int years) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.ageTracker != null, $"pawn '{nickname}' has no age tracker");
    ctx.Require(years >= 0, $"an age of {years} is not a real age");

    pawn.ageTracker!.AgeBiologicalTicks = (long)years * GenDate.TicksPerYear;
  }

  /// <summary>Sets a pawn's gender to male or female.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to change.</param>
  /// <param name="gender">Either <c>male</c> or <c>female</c>, case insensitive.</param>
  [Given("{string} gender is {word}")]
  public void SetGender(PickleContext ctx, string nickname, string gender) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);

    pawn.gender = gender.ToLowerInvariant() switch {
      "male" => Gender.Male,
      "female" => Gender.Female,
      _ => throw new ArgumentException($"unknown gender '{gender}'; supported: male, female"),
    };
  }

  /// <summary>
  /// Sets a pawn's body type and redraws the pawn with it. The body type is independent of the
  /// gender, so a female pawn can be <c>Fat</c>, <c>Thin</c> or <c>Hulk</c>, and the age is not
  /// checked against it. The change lasts only until the game chooses again: adding or removing a
  /// Biotech gene that carries a body type (<c>Body_Fat</c>, <c>Body_Thin</c>, <c>Body_Hulk</c>)
  /// re-applies one, and so does a child growing up.
  /// </summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to change.</param>
  /// <param name="bodyTypeDefName">The body type def to set, case insensitive, such as <c>Fat</c>.</param>
  [Given("{string} body type is {word}")]
  public void SetBodyType(PickleContext ctx, string nickname, string bodyTypeDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Require(pawn.story != null, $"pawn '{nickname}' has no story, so it has no body type");

    List<BodyTypeDef> all = DefDatabase<BodyTypeDef>.AllDefsListForReading;
    BodyTypeDef? bodyType = all.FirstOrDefault(
        b => string.Equals(b.defName, bodyTypeDefName, StringComparison.OrdinalIgnoreCase));
    ctx.Require(
        bodyType != null,
        $"no body type '{bodyTypeDefName}'; the body types are {string.Join(", ", all.Select(b => b.defName))}");

    pawn.story!.bodyType = bodyType!;
    pawn.Drawer.renderer.SetAllGraphicsDirty();
  }

  /// <summary>
  /// Asserts the graphic the game draws a pawn's body with, which follows the body type. Reading it
  /// initializes the pawn's render tree, as drawing would.
  /// </summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to check.</param>
  /// <param name="texturePath">The texture path, such as <c>Things/Pawn/Humanlike/Bodies/Naked_Fat</c>.</param>
  [Then("{string} body is drawn from {string}")]
  public void AssertBodyDrawnFrom(PickleContext ctx, string nickname, string texturePath) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    pawn.Drawer.renderer.renderTree.EnsureInitialized(PawnRenderFlags.None);
    string? drawn = pawn.Drawer.renderer.BodyGraphic?.path;

    ctx.Assert(
        drawn == texturePath,
        $"pawn '{nickname}' body should be drawn from '{texturePath}'; it is drawn from " +
        $"'{drawn ?? "(no graphic)"}' with body type {pawn.story?.bodyType?.defName ?? "(none)"}");
  }

  /// <summary>Sets a pawn's passion for a skill. Refuses a skill the pawn cannot use at all, where a passion would mean nothing.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="passion">One of none, minor or major.</param>
  /// <param name="skillDefName">The skill def.</param>
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

  /// <summary>Checks the work type is not disabled for the pawn. The failure prints the backstories and traits that decide it.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="workDefName">The work type def.</param>
  [Then("{string} can do {string}")]
  public void AssertCanDo(PickleContext ctx, string nickname, string workDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    WorkTypeDef work = DefLookup.Require<WorkTypeDef>(workDefName);

    ctx.Assert(
        !pawn.WorkTypeIsDisabled(work),
        $"pawn '{nickname}' cannot do '{workDefName}'. {DescribeStory(pawn)}");
  }

  /// <summary>Checks the work type is disabled for the pawn.</summary>
  /// <param name="ctx">The running step's context.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="workDefName">The work type def.</param>
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
