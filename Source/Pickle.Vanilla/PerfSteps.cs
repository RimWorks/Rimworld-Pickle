using System.Globalization;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Runtime;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// Budgets over what the simulation costs. Only fast mode drives the tick loop through
/// Pickle, so a watch-mode scenario samples nothing and these steps say so.
/// </summary>
[PickleSteps]
public class PerfSteps {
  [Then("the last {int} ticks average under {float} ms")]
  public void MeanUnder(PickleContext ctx, int ticks, float budgetMs) {
    TickCostWindow window = Require(ctx, ticks);
    ctx.Assert(window.MeanMs < budgetMs, window.MeanMs < budgetMs ? null : Describe(window, ticks, budgetMs, "averaged", window.MeanMs));
  }

  [Then("no tick in the last {int} took more than {float} ms")]
  public void MaxUnder(PickleContext ctx, int ticks, float budgetMs) {
    TickCostWindow window = Require(ctx, ticks);
    bool passed = window.MaxMs <= budgetMs;
    ctx.Assert(passed, passed ? null : Describe(window, ticks, budgetMs, "peaked at", window.MaxMs));
  }

  // Fewer samples than the budget asked for has measured nothing. Passing on twelve ticks
  // is how a broken assert lives in a suite for a year.
  private static TickCostWindow Require(PickleContext ctx, int ticks) {
    TickCostWindow window = TickCostSampler.Window(ticks);

    ctx.Require(
        PickleRunMode.Current == PickleRunMode.Mode.Fast,
        "tick budgets only measure in fast mode; watch mode leaves the tick loop to the game");

    ctx.Require(
        window.Count >= ticks,
        $"only {window.Count} of the last {ticks} ticks were sampled; "
        + "add a longer 'I wait N ticks' before this step");

    return window;
  }

  private static string Describe(TickCostWindow window, int ticks, float budgetMs, string verb, double actualMs) {
    return $"the last {ticks} ticks {verb} {Ms(actualMs)}ms, over the {Budget(budgetMs)}ms budget"
        + $"\n  sampled {window.Count} ticks, mean {Ms(window.MeanMs)}ms, "
        + $"max {Ms(window.MaxMs)}ms at {window.MaxIndexFromEnd} ticks from the end";
  }

  private static string Ms(double value) {
    return value.ToString("0.###", CultureInfo.InvariantCulture);
  }

  // Echoed as written, not rounded. A budget of 0.0001 printed as "0ms" reads as a bug
  // in the step rather than as the number the scenario asked for.
  private static string Budget(float value) {
    return value.ToString(CultureInfo.InvariantCulture);
  }
}
