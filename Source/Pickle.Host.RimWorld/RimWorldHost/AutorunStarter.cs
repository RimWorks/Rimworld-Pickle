using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Autorun;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.RimWorldHost;

[StaticConstructorOnStartup]
internal static class AutorunStarter {
  static AutorunStarter() {
    Func<Task>? run = AutorunBootstrap.Start();
    if (run == null) {
      return;
    }

    try {
      LongEventHandler.QueueLongEvent(() => _ = run(), "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "autorun failed to start");
    }
  }
}
