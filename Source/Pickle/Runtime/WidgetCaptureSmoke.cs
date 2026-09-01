using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Input;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

public static class WidgetCaptureSmoke {
  public static void Run() {
    PickleDriver.EnsureExists();
    LongEventHandler.QueueLongEvent(() => _ = RunSmoke(), "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
  }

  private static async Task RunSmoke() {
    try {
      PickleDriver driver = PickleDriver.Instance;

      TagStore.SessionActive = true;

      Dialog_MessageBox dialog = new Dialog_MessageBox("pickle widget capture test");
      Find.WindowStack.Add(dialog);
      await driver.WaitFrames(2);

      bool found = TagStore.TryGet("btn:OK", out Rect _, out bool duplicate);

      if (!found) {
        string knownTags = string.Join(", ", TagStore.KnownTags);
        Log.Error("pickle: widget capture smoke failed: btn:OK not found. known tags: {KnownTags}", [knownTags]);
        dialog.Close(false);
        return;
      }

      if (duplicate) {
        Log.Error("pickle: widget capture smoke failed: btn:OK is ambiguous (duplicate)");
        dialog.Close(false);
        return;
      }

      // Click through ctx.Click, not the raw injector: the shipped path is tag
      // lookup -> rect -> input, and that is what "I click button {string}" uses.
      PickleContext ctx = new PickleContext();
      try {
        await ctx.Click("btn:OK");
      } catch (Exception clickEx) {
        Log.Error(clickEx, "pickle: widget capture smoke failed: ctx.Click(\"btn:OK\") threw");
        dialog.Close(false);
        return;
      }

      bool closed = !Find.WindowStack.IsOpen<Dialog_MessageBox>();
      if (!closed) {
        Log.Error("pickle: widget capture smoke failed: click did not close the dialog");
        dialog.Close(false);
        return;
      }

      Log.Info("pickle: widget capture smoke passed");
    } catch (Exception ex) {
      Log.Error(ex, "pickle: widget capture smoke failed with exception");
    }
  }
}
