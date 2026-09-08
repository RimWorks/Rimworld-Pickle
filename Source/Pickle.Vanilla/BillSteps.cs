using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Bills on a workbench, and the work priorities that decide who fills them.</summary>
[PickleSteps]
public class BillSteps {
  /// <summary>Adds a bill for a recipe to the first bench on the map that takes bills and can make it.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="recipeDefName">The recipe to add.</param>
  /// <param name="benchDefName">The workbench def to add it to.</param>
  [When("I add bill {string} to the {string}")]
  public void AddBill(PickleContext ctx, string recipeDefName, string benchDefName) {
    AddBillTo(ctx, recipeDefName, RequireBench(ctx, benchDefName));
  }

  /// <summary>Adds a bill for a recipe to the bench standing at a specific cell.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="recipeDefName">The recipe to add.</param>
  /// <param name="benchDefName">The workbench def expected at the cell.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  [When("I add bill {string} to the {string} at \\({int}, {int}\\)")]
  public void AddBillAt(PickleContext ctx, string recipeDefName, string benchDefName, int x, int z) {
    ThingDef def = DefLookup.Require<ThingDef>(benchDefName);
    Map map = MapLookup.RequireMap(ctx);
    Thing thing = MapLookup.RequireThingAt(ctx, map, new IntVec3(x, 0, z), def);

    ctx.Require(thing is IBillGiver, $"the {benchDefName} at ({x}, {z}) takes no bills");
    AddBillTo(ctx, recipeDefName, (IBillGiver)thing);
  }

  /// <summary>Asserts a bench's bill stack holds a specific number of bills.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="benchDefName">The workbench def to check.</param>
  /// <param name="expected">The bill count expected.</param>
  [Then("the {string} has {int} bills")]
  public void AssertBillCount(PickleContext ctx, string benchDefName, int expected) {
    IBillGiver bench = RequireBench(ctx, benchDefName);
    int actual = bench.BillStack?.Count ?? 0;

    ctx.Assert(
        actual == expected,
        $"the {benchDefName} should have {expected} bills; has {actual}. {DescribeBills(bench)}");
  }

  /// <summary>Sets a pawn's priority for a work type, turning manual priorities on first since the game
  /// collapses anything but 0 or 3 while they are off.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to change.</param>
  /// <param name="workDefName">The work type to set.</param>
  /// <param name="priority">The priority to set, 0 to disable it.</param>
  [When("I set {string} priority {string} to {int}")]
  public void SetPriority(PickleContext ctx, string nickname, string workDefName, int priority) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    WorkTypeDef work = DefLookup.Require<WorkTypeDef>(workDefName);
    ctx.Require(pawn.workSettings != null, $"pawn '{nickname}' has no work settings");

    // SetPriority throws on a tracker that was never initialised.
    pawn.workSettings!.EnableAndInitializeIfNotAlreadyInitialized();

    ctx.Require(
        !pawn.WorkTypeIsDisabled(work),
        $"pawn '{nickname}' cannot do '{workDefName}', so its priority would stay 0");

    EnableManualPriorities();
    pawn.workSettings.SetPriority(work, priority);
  }

  /// <summary>Asserts a pawn's priority for a work type.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to check.</param>
  /// <param name="workDefName">The work type to check.</param>
  /// <param name="expected">The priority expected.</param>
  [Then("{string} priority {string} is {int}")]
  public void AssertPriority(PickleContext ctx, string nickname, string workDefName, int expected) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    WorkTypeDef work = DefLookup.Require<WorkTypeDef>(workDefName);
    ctx.Require(pawn.workSettings != null, $"pawn '{nickname}' has no work settings");

    int actual = pawn.workSettings!.GetPriority(work);
    string disabled = pawn.WorkTypeIsDisabled(work) ? ", and the pawn cannot do that work at all" : string.Empty;

    ctx.Assert(
        actual == expected,
        $"pawn '{nickname}' priority for '{workDefName}' should be {expected}; is {actual}{disabled}");
  }

  /// <summary>Waits for a recipe's product count on the map to rise above what it was when the wait started.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="recipeDefName">The recipe whose product to watch for.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // A bill has no finished event, so this watches the recipe's product instead. The count is
  // read first, because the map may already hold some.
  [When("I wait for bill {string} to finish", TimeoutSeconds = 125f)]
  public async Task WaitForBill(PickleContext ctx, string recipeDefName) {
    RecipeDef recipe = DefLookup.Require<RecipeDef>(recipeDefName);
    ThingDef? maybeProduct = recipe.products?.FirstOrDefault()?.thingDef;
    ctx.Require(maybeProduct != null, $"recipe '{recipeDefName}' makes no thing, so nothing can be counted");
    ThingDef product = maybeProduct!;

    Map map = MapLookup.RequireMap(ctx);
    int before = CountOf(map, product);

    await ctx.AssertEventually(
        () => CountOf(map, product) > before,
        () => $"no {product.defName} was made; the map held {before} before and {CountOf(map, product)} after",
        120f);
  }

  // With manual priorities off the game keeps only 0 or 3, so setting 1 would read back as
  // 3. Turning them on is what a player does before using the numbers.
  private static void EnableManualPriorities() {
    PlaySettings? settings = Find.PlaySettings;
    if (settings == null || settings.useWorkPriorities) {
      return;
    }

    settings.useWorkPriorities = true;
    foreach (Pawn colonist in PawnsFinder.AllMaps_FreeColonists) {
      colonist.workSettings?.Notify_UseWorkPrioritiesChanged();
    }
  }

  private static void AddBillTo(PickleContext ctx, string recipeDefName, IBillGiver bench) {
    RecipeDef recipe = DefLookup.Require<RecipeDef>(recipeDefName);
    Thing thing = (Thing)bench;

    ctx.Require(
        thing.def.AllRecipes.Contains(recipe),
        $"the {thing.def.defName} cannot make '{recipeDefName}'; it does {DescribeRecipes(thing.def)}");

    bench.BillStack.AddBill(BillUtility.MakeNewBill(recipe, null));
  }

  private static IBillGiver RequireBench(PickleContext ctx, string benchDefName) {
    ThingDef def = DefLookup.Require<ThingDef>(benchDefName);
    Map map = MapLookup.RequireMap(ctx);

    IBillGiver? bench = map.listerThings.ThingsOfDef(def).OfType<IBillGiver>().FirstOrDefault();
    ctx.Require(bench != null, $"the map has no {benchDefName} that takes bills");

    return bench!;
  }

  private static int CountOf(Map map, ThingDef def) {
    return map.listerThings.ThingsOfDef(def).Sum(t => t.stackCount);
  }

  private static string DescribeBills(IBillGiver bench) {
    List<string> bills = [.. bench.BillStack?.Bills.Select(b => b.recipe?.defName ?? "(no recipe)") ?? []];
    return bills.Count == 0 ? "bills: (none)" : $"bills: {string.Join(", ", bills)}";
  }

  private static string DescribeRecipes(ThingDef def) {
    List<string> names = [.. def.AllRecipes.Select(r => r.defName).Take(6)];
    return names.Count == 0 ? "(no recipes)" : string.Join(", ", names);
  }
}
