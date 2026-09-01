using System;
using System.Threading.Tasks;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

public static class PumpSmoke {
  public static async Task Run() {
    PickleContext ctx = new PickleContext();
    try {
      await RunAsync(ctx);
      Log.Info("pickle: pump smoke passed");
    } catch (Exception ex) {
      Log.Error(ex, "pickle: pump smoke failed");
    }
  }

  public static async Task RunAsync(PickleContext ctx) {
    int startTicksGame = Find.TickManager.TicksGame;
    await ctx.WaitTicks(10);
    ctx.Assert(Find.TickManager.TicksGame - startTicksGame >= 10, "PumpSmoke: TicksGame advanced by at least 10");

    await ctx.WaitUntil(() => true, 5f);
    await ctx.WaitFrames(2);
  }
}
