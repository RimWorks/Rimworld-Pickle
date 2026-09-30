using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Input;
using RimWorld;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Proves key and click input against a real vanilla dialog: a key event closes it through the
/// UIRootOnGUI reinvoke, then a click on its OK button closes it through the widget patch.
/// </summary>
public static class EventSynthSmoke {
  /// <summary>Queues the smoke on the loading long event, so it runs once the game is ready for windows.</summary>
  public static void Run() {
    PickleDriver.EnsureExists();
    LongEventHandler.QueueLongEvent(() => _ = RunSmoke(), "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
  }

  private static async Task RunSmoke() {
    try {
      PickleDriver driver = PickleDriver.Instance;

      EventSynth.SuppressDebugLogAutoOpen();
      TagStore.SessionActive = true;

      if (!await EscapeClosesDialog(driver)) {
        TagStore.SessionActive = false;
        return;
      }

      if (!await ClickOnOkClosesDialog(driver)) {
        TagStore.SessionActive = false;
        return;
      }

      TagStore.SessionActive = false;
      Log.InfoTo(PickleLog.Channel, "event synth smoke passed");
    } catch (Exception ex) {
      TagStore.SessionActive = false;
      Log.ErrorTo(PickleLog.Channel, ex, "event synth smoke failed with exception");
    }
  }

  private static async Task<bool> EscapeClosesDialog(PickleDriver driver) {
    Dialog_MessageBox dialog = new Dialog_MessageBox("pickle synth test (key)");
    Find.WindowStack.Add(dialog);
    await driver.WaitFrames(2);

    PickleContext ctx = new PickleContext();
    await ctx.PressKey("Escape");

    if (Find.WindowStack.IsOpen<Dialog_MessageBox>()) {
      Log.ErrorTo(PickleLog.Channel, "event synth smoke failed: Escape did not close the dialog");
      dialog.Close(false);
      await driver.WaitFrames(1);
      return false;
    }

    return true;
  }

  private static async Task<bool> ClickOnOkClosesDialog(PickleDriver driver) {
    Dialog_MessageBox dialog = new Dialog_MessageBox("pickle synth test");
    Find.WindowStack.Add(dialog);
    await driver.WaitFrames(2);

    PickleContext ctx = new PickleContext();

    try {
      await ctx.Click("btn:OK");
    } catch (InvalidOperationException ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "event synth smoke failed: click on 'btn:OK' threw");
      dialog.Close(false);
      await driver.WaitFrames(1);
      return false;
    }

    if (Find.WindowStack.IsOpen<Dialog_MessageBox>()) {
      Log.ErrorTo(PickleLog.Channel, "event synth smoke failed: the click did not close the dialog");
      dialog.Close(false);
      await driver.WaitFrames(1);
      return false;
    }

    return true;
  }
}
