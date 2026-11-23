using System;
using System.Threading.Tasks;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>Exercises the async wait pump directly, without a feature file, and logs the result.</summary>
public static class PumpSmoke {
  /// <summary>Runs the smoke and logs whether it passed, swallowing any exception.</summary>
  /// <returns>A task that completes when the smoke finishes.</returns>
  public static async Task Run() {
    PickleContext ctx = new PickleContext();
    try {
      await RunAsync(ctx);
      Log.InfoTo(PickleLog.Channel, "pump smoke passed");
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "pump smoke failed");
    }
  }

  /// <summary>Waits on ticks, a condition and frames in turn, asserting the tick wait advanced the clock.</summary>
  /// <param name="ctx">The context to run the waits against.</param>
  /// <returns>A task that completes when the smoke finishes.</returns>
  public static async Task RunAsync(PickleContext ctx) {
    int startTicksGame = Find.TickManager.TicksGame;
    await ctx.WaitTicks(10);
    ctx.Assert(Find.TickManager.TicksGame - startTicksGame >= 10, "PumpSmoke: TicksGame advanced by at least 10");

    await ctx.WaitUntil(() => true, 5f);
    await ctx.WaitFrames(2);
  }
}
