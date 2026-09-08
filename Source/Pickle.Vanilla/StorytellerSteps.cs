using System;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// The storyteller, the difficulty, and colony wealth, which together drive raid size and
/// incident pacing.
/// </summary>
[PickleSteps]
public class StorytellerSteps {
  /// <summary>Asserts the live storyteller matches a def.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="storytellerDefName">The storyteller def expected.</param>
  [Then("the storyteller is {string}")]
  public void AssertStoryteller(PickleContext ctx, string storytellerDefName) {
    Storyteller storyteller = RequireStoryteller(ctx);
    StorytellerDef def = DefLookup.Require<StorytellerDef>(storytellerDefName);

    ctx.Assert(
        storyteller.def == def,
        storyteller.def == def ? null : $"the storyteller should be '{storytellerDefName}'; it is '{storyteller.def.defName}'");
  }

  /// <summary>Swaps the def on the live storyteller.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="storytellerDefName">The storyteller def to switch to.</param>
  [When("I set the storyteller to {string}")]
  public void SetStoryteller(PickleContext ctx, string storytellerDefName) {
    Storyteller storyteller = RequireStoryteller(ctx);
    StorytellerDef def = DefLookup.Require<StorytellerDef>(storytellerDefName);

    if (storyteller.def != def) {
      storyteller.def = def;

      // Vanilla's own storyteller picker calls this only when the def actually changed. It
      // rebuilds the comp list from the new def, which a bare field write skips.
      storyteller.Notify_DefChanged();
    }
  }

  /// <summary>Asserts the live difficulty matches a def.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="difficultyDefName">The difficulty def expected.</param>
  [Then("the difficulty is {string}")]
  public void AssertDifficulty(PickleContext ctx, string difficultyDefName) {
    Storyteller storyteller = RequireStoryteller(ctx);
    DifficultyDef def = DefLookup.Require<DifficultyDef>(difficultyDefName);

    ctx.Assert(
        storyteller.difficultyDef == def,
        storyteller.difficultyDef == def
            ? null
            : $"the difficulty should be '{difficultyDefName}'; it is '{storyteller.difficultyDef.defName}'");
  }

  /// <summary>Asserts total colony wealth is above a threshold.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="threshold">The value wealth must exceed.</param>
  [Then("colony wealth is above {float}")]
  public void AssertWealthAbove(PickleContext ctx, float threshold) {
    AssertWealth(ctx, "colony wealth", w => w.WealthTotal, threshold, actual => actual > threshold, "above");
  }

  /// <summary>Asserts total colony wealth is below a threshold.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="threshold">The value wealth must fall under.</param>
  [Then("colony wealth is below {float}")]
  public void AssertWealthBelow(PickleContext ctx, float threshold) {
    AssertWealth(ctx, "colony wealth", w => w.WealthTotal, threshold, actual => actual < threshold, "below");
  }

  /// <summary>Asserts colony wealth in one category (items, buildings, or floors) is above a threshold.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="category">The wealth category to read.</param>
  /// <param name="threshold">The value wealth in that category must exceed.</param>
  [Then("colony wealth in {word} is above {float}")]
  public void AssertWealthInCategoryAbove(PickleContext ctx, string category, float threshold) {
    AssertWealth(ctx, $"colony wealth in {category}", w => WealthCategory(w, category), threshold, actual => actual > threshold, "above");
  }

  // WealthWatcher only recounts on its own every 5000 ticks (RecountIfNeeded), so a read
  // straight after a spawn sees the pre-spawn number. ForceRecount is the public escape hatch.
  private static void AssertWealth(
      PickleContext ctx, string subject, Func<WealthWatcher, float> read, float threshold, Func<float, bool> holds, string comparison) {
    Map map = MapLookup.RequireMap(ctx);
    WealthWatcher watcher = map.wealthWatcher;
    watcher.ForceRecount();
    float actual = read(watcher);

    ctx.Assert(
        holds(actual),
        holds(actual)
            ? null
            : $"{subject} should be {comparison} {threshold}; it is {actual:F1} (items {watcher.WealthItems:F0}, " +
              $"buildings {watcher.WealthBuildings:F0}, floors {watcher.WealthFloorsOnly:F0}, pawns {watcher.WealthPawns:F0})");
  }

  private static float WealthCategory(WealthWatcher watcher, string category) {
    return category.ToLowerInvariant() switch {
      "items" => watcher.WealthItems,
      "buildings" => watcher.WealthBuildings,
      "floors" => watcher.WealthFloorsOnly,
      _ => throw new InvalidOperationException($"'{category}' is not a wealth category; try items, buildings, or floors"),
    };
  }

  private static Storyteller RequireStoryteller(PickleContext ctx) {
    Storyteller? storyteller = Find.Storyteller;
    ctx.Require(storyteller != null, "no storyteller is running; load a save first with 'the save ... is loaded'");
    return storyteller!;
  }
}
