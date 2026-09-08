using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Individual thoughts, opinions and relations, which the mood total hides.</summary>
[PickleSteps]
public class ThoughtSteps {
  /// <summary>Asserts a pawn carries a thought with the given def, mood or social. Waits first, since a situational thought is recalculated on an interval.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="thoughtDefName">The thought def to look for.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // A situational thought is recalculated on an interval rather than on the change that
  // caused it, so an immediate read races the game.
  [Then("{string} has thought {string}")]
  public async Task AssertThought(PickleContext ctx, string nickname, string thoughtDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThoughtDef def = DefLookup.Require<ThoughtDef>(thoughtDefName);
    RequireThoughts(ctx, pawn, nickname);

    await ctx.AssertEventually(
        () => HasThought(pawn, def),
        () => $"pawn '{nickname}' should have thought '{thoughtDefName}'; {DescribeThoughts(pawn)}");
  }

  /// <summary>Asserts a pawn does not carry a thought with the given def, mood or social.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="thoughtDefName">The thought def that should be absent.</param>
  [Then("{string} has no thought {string}")]
  public void AssertNoThought(PickleContext ctx, string nickname, string thoughtDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThoughtDef def = DefLookup.Require<ThoughtDef>(thoughtDefName);
    RequireThoughts(ctx, pawn, nickname);

    ctx.Assert(
        !HasThought(pawn, def),
        $"pawn '{nickname}' should not have thought '{thoughtDefName}'; {DescribeThoughts(pawn)}");
  }

  /// <summary>Asserts the summed mood offset of a mood thought matches an expected value, within tolerance.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="thoughtDefName">The mood thought def to read.</param>
  /// <param name="expected">The expected mood offset.</param>
  [Then("{string} thought {string} mood offset is {float}")]
  public void AssertMoodOffset(PickleContext ctx, string nickname, string thoughtDefName, float expected) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThoughtDef def = DefLookup.Require<ThoughtDef>(thoughtDefName);
    RequireThoughts(ctx, pawn, nickname);

    List<Thought> found = MoodThoughtsOf(pawn, def);
    ctx.Require(
        found.Count > 0,
        $"pawn '{nickname}' carries no mood thought '{thoughtDefName}'. a social thought moves " +
        $"opinion rather than mood, so read it with 'opinion of'. {DescribeThoughts(pawn)}");

    float actual = found.Sum(t => t.MoodOffset());
    ctx.Assert(
        StatTolerance.IsNear(actual, expected),
        $"pawn '{nickname}' thought '{thoughtDefName}' should offset mood by {expected} " +
        $"within {StatTolerance.For(expected)}; actual {actual}. {DescribeThoughts(pawn)}");
  }

  /// <summary>Gives a pawn a mood thought.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="thoughtDefName">The thought def to give.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("{string} is given thought {string}")]
  public async Task GiveThought(PickleContext ctx, string nickname, string thoughtDefName) {
    await GainMemory(ctx, nickname, thoughtDefName, null);
  }

  /// <summary>Gives a pawn a social thought about another pawn.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn's nickname.</param>
  /// <param name="thoughtDefName">The thought def to give.</param>
  /// <param name="otherNickname">The other pawn the thought is about.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("{string} is given thought {string} about {string}")]
  public async Task GiveSocialThought(
      PickleContext ctx, string nickname, string thoughtDefName, string otherNickname) {
    await GainMemory(ctx, nickname, thoughtDefName, otherNickname);
  }

  /// <summary>Asserts one pawn's opinion of another is an exact value.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn whose opinion is read.</param>
  /// <param name="otherNickname">The pawn the opinion is of.</param>
  /// <param name="expected">The expected opinion value.</param>
  [Then("{string} opinion of {string} is {int}")]
  public void AssertOpinion(PickleContext ctx, string nickname, string otherNickname, int expected) {
    AssertOpinionThat(ctx, nickname, otherNickname, actual => actual == expected, $"should be {expected}");
  }

  /// <summary>Records one pawn's current opinion of another, for a later "rose" comparison.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn whose opinion is remembered.</param>
  /// <param name="otherNickname">The pawn the opinion is of.</param>
  // Net opinion sums the relation with whatever traits the pawn rolled, and a random pair
  // can cancel a relation exactly. Reading before and after holds the traits constant.
  [Given("I remember {string} opinion of {string}")]
  public void RememberOpinion(PickleContext ctx, string nickname, string otherNickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Pawn other = PawnLookup.RequireLiving(otherNickname);

    ctx.Require(pawn.relations != null, $"pawn '{nickname}' has no relations tracker");
    ctx.Set(new RememberedOpinion(nickname, otherNickname, pawn.relations!.OpinionOf(other)));
  }

  /// <summary>Asserts one pawn's opinion of another rose since it was last remembered.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn whose opinion is read.</param>
  /// <param name="otherNickname">The pawn the opinion is of.</param>
  [Then("{string} opinion of {string} rose")]
  public void AssertOpinionRose(PickleContext ctx, string nickname, string otherNickname) {
    RememberedOpinion before = RequireRemembered(ctx, nickname, otherNickname);
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Pawn other = PawnLookup.RequireLiving(otherNickname);

    int actual = pawn.relations!.OpinionOf(other);
    bool rose = actual > before.Value;

    ctx.Assert(
        rose,
        rose ? null : $"'{nickname}' opinion of '{otherNickname}' should have risen from " +
            $"{before.Value}; actual {actual}. {pawn.relations.OpinionExplanation(other)}");
  }

  /// <summary>Asserts one pawn's opinion of another is above a bound.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn whose opinion is read.</param>
  /// <param name="otherNickname">The pawn the opinion is of.</param>
  /// <param name="bound">The lower bound, exclusive.</param>
  [Then("{string} opinion of {string} is above {int}")]
  public void AssertOpinionAbove(PickleContext ctx, string nickname, string otherNickname, int bound) {
    AssertOpinionThat(ctx, nickname, otherNickname, actual => actual > bound, $"should be above {bound}");
  }

  /// <summary>Asserts one pawn's opinion of another is below a bound.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The pawn whose opinion is read.</param>
  /// <param name="otherNickname">The pawn the opinion is of.</param>
  /// <param name="bound">The upper bound, exclusive.</param>
  [Then("{string} opinion of {string} is below {int}")]
  public void AssertOpinionBelow(PickleContext ctx, string nickname, string otherNickname, int bound) {
    AssertOpinionThat(ctx, nickname, otherNickname, actual => actual < bound, $"should be below {bound}");
  }

  /// <summary>Asserts a relation def holds between two pawns.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The first pawn's nickname.</param>
  /// <param name="otherNickname">The second pawn's nickname.</param>
  /// <param name="relationDefName">The relation def expected between them.</param>
  [Then("{string} and {string} are {string}")]
  public void AssertRelation(
      PickleContext ctx, string nickname, string otherNickname, string relationDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Pawn other = PawnLookup.RequireLiving(otherNickname);
    PawnRelationDef def = DefLookup.Require<PawnRelationDef>(relationDefName);

    ctx.Assert(
        PawnRelationUtility.GetRelations(pawn, other).Contains(def),
        $"'{nickname}' and '{otherNickname}' should be '{relationDefName}'; {DescribeRelations(pawn, other)}");
  }

  /// <summary>Adds a direct relation between two pawns.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="nickname">The first pawn's nickname.</param>
  /// <param name="otherNickname">The second pawn's nickname.</param>
  /// <param name="relationDefName">The relation def to add. Must not be an implied relation.</param>
  [When("I make {string} and {string} {string}")]
  public void MakeRelation(
      PickleContext ctx, string nickname, string otherNickname, string relationDefName) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Pawn other = PawnLookup.RequireLiving(otherNickname);
    PawnRelationDef def = DefLookup.Require<PawnRelationDef>(relationDefName);

    ctx.Require(pawn != other, $"'{nickname}' cannot hold a relation to itself");
    ctx.Require(
        !def.implied,
        $"'{relationDefName}' is implied by other relations rather than stored, so it cannot be set. " +
        "set the relations it follows from instead");
    ctx.Require(
        !pawn.relations.DirectRelationExists(def, other),
        $"'{nickname}' and '{otherNickname}' are already '{relationDefName}'");

    pawn.relations.AddDirectRelation(def, other);
  }

  private static async Task GainMemory(
      PickleContext ctx, string nickname, string thoughtDefName, string? otherNickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ThoughtDef def = DefLookup.Require<ThoughtDef>(thoughtDefName);
    Pawn? other = otherNickname == null ? null : PawnLookup.RequireLiving(otherNickname);
    RequireThoughts(ctx, pawn, nickname);

    pawn.needs.mood.thoughts.memories.TryGainMemory(def, other, null);

    // A nullifying trait or precept makes TryGainMemory a no-op, so prove it landed.
    await ctx.AssertEventually(
        () => HasThought(pawn, def),
        () => $"'{thoughtDefName}' never landed on '{nickname}'. a nullifying trait or precept " +
            $"drops a memory silently. {DescribeThoughts(pawn)}");
  }

  private static void AssertOpinionThat(
      PickleContext ctx, string nickname, string otherNickname, Func<int, bool> holds, string wanted) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Pawn other = PawnLookup.RequireLiving(otherNickname);

    ctx.Require(pawn.relations != null, $"pawn '{nickname}' has no relations tracker");
    int actual = pawn.relations!.OpinionOf(other);

    ctx.Assert(
        holds(actual),
        $"'{nickname}' opinion of '{otherNickname}' {wanted}; actual {actual}. " +
        $"{pawn.relations.OpinionExplanation(other)}");
  }

  private static void RequireThoughts(PickleContext ctx, Pawn pawn, string nickname) {
    ctx.Require(
        pawn.needs?.mood?.thoughts != null,
        $"pawn '{nickname}' has no mood need, so it carries no thoughts");
  }

  // A social memory lives in the memory list without ever being a mood thought, so neither
  // list alone answers the question.
  private static bool HasThought(Pawn pawn, ThoughtDef def) {
    ThoughtHandler thoughts = pawn.needs.mood.thoughts;
    return thoughts.memories.NumMemoriesOfDef(def) > 0 || MoodThoughtsOf(pawn, def).Count > 0;
  }

  // A memory def has no ThoughtWorker, so GetMoodThoughtsFor logs an NRE building it.
  private static List<Thought> MoodThoughtsOf(Pawn pawn, ThoughtDef def) {
    List<Thought> found = [];
    if (def.IsSituational) {
      pawn.needs.mood.thoughts.GetMoodThoughtsFor(def, found);
      return found;
    }

    found.AddRange(pawn.needs.mood.thoughts.memories.Memories.Where(m => m.def == def));
    return found;
  }

  private static string DescribeThoughts(Pawn pawn) {
    List<Thought> mood = [];
    pawn.needs.mood.thoughts.GetAllMoodThoughts(mood);

    List<string> lines = [.. mood.Select(t => $"{t.def.defName} ({t.MoodOffset():+0.#;-0.#;0})")];
    foreach (Thought_Memory memory in pawn.needs.mood.thoughts.memories.Memories) {
      if (memory is ISocialThought) {
        lines.Add($"{memory.def.defName} (social, about {memory.otherPawn?.LabelShort ?? "nobody"})");
      }
    }

    return lines.Count == 0 ? "the pawn holds no thoughts" : $"thoughts: {string.Join(", ", lines)}";
  }

  private static string DescribeRelations(Pawn pawn, Pawn other) {
    List<string> names = [.. PawnRelationUtility.GetRelations(pawn, other).Select(r => r.defName)];
    return names.Count == 0 ? "they hold no relation to each other" : $"relations: {string.Join(", ", names)}";
  }

  private static RememberedOpinion RequireRemembered(
      PickleContext ctx, string nickname, string otherNickname) {
    RememberedOpinion? before = null;
    try {
      before = ctx.Get<RememberedOpinion>();
    } catch (InvalidOperationException) {
      // nothing remembered, reported below with the step that was missed
    }

    ctx.Require(
        before != null,
        $"no opinion was remembered, so there is nothing to compare against. " +
        $"use 'I remember \"{nickname}\" opinion of \"{otherNickname}\"' first");
    ctx.Require(
        before!.Of == nickname && before.About == otherNickname,
        $"the remembered opinion was '{before.Of}' of '{before.About}', " +
        $"not '{nickname}' of '{otherNickname}'");

    return before;
  }

  private sealed class RememberedOpinion {
    public RememberedOpinion(string of, string about, int value) {
      Of = of;
      About = about;
      Value = value;
    }

    public string Of { get; }

    public string About { get; }

    public int Value { get; }
  }
}
