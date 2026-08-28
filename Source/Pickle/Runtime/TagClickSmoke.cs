using System;
using Pickle.Input;
using Verse;

namespace Pickle.Runtime;

internal static class TagClickSmoke {
  internal static async void Run() {
    try {
      PickleDriver bootDriver = PickleDriver.Instance;
      await bootDriver.WaitFrames(1);

      TagStore.SessionActive = true;

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitFrames(1);

      TagClickTestWindow.Clicked = false;
      TagClickTestWindow testWindow = new TagClickTestWindow();
      Find.WindowStack.Add(testWindow);

      await driver.WaitFrames(3);

      bool tagExists = TagStore.TryGet("pickle-smoke:btn", out _, out bool isDuplicate);
      if (!tagExists) {
        Log.Error("pickle: tag click smoke failed - tag not recorded after window draw");
        TagStore.SessionActive = false;
        Find.WindowStack.TryRemove(testWindow);
        return;
      }

      if (isDuplicate) {
        Log.Error("pickle: tag click smoke failed - tag marked as duplicate; Repaint guard not working");
        TagStore.SessionActive = false;
        Find.WindowStack.TryRemove(testWindow);
        return;
      }

      PickleContext ctx = new PickleContext();

      try {
        if (!XdoInput.Available) {
          Log.Warning("pickle: tag click smoke skipped - xdotool not available");
          TagStore.SessionActive = false;
          Find.WindowStack.TryRemove(testWindow);
          return;
        }

        await ctx.Click("pickle-smoke:btn");
        await driver.WaitFrames(2);
      } catch (InvalidOperationException clickEx) {
        Log.Error($"pickle: tag click smoke failed - Click threw exception: {clickEx.Message}");
        TagStore.SessionActive = false;
        Find.WindowStack.TryRemove(testWindow);
        return;
      }

      if (!TagClickTestWindow.Clicked) {
        Log.Error("pickle: tag click smoke failed - click did not fire widget handler");
        TagStore.SessionActive = false;
        Find.WindowStack.TryRemove(testWindow);
        return;
      }

      try {
        await ctx.Click("pickle-smoke:does-not-exist");
        Log.Error("pickle: tag click smoke failed - should have thrown InvalidOperationException for missing tag");
        TagStore.SessionActive = false;
        Find.WindowStack.TryRemove(testWindow);
        return;
      } catch (InvalidOperationException missEx) {
        if (!missEx.Message.Contains("pickle-smoke:does-not-exist")) {
          Log.Error($"pickle: tag click smoke failed - error message missing tag name: {missEx.Message}");
          TagStore.SessionActive = false;
          Find.WindowStack.TryRemove(testWindow);
          return;
        }

        if (!missEx.Message.Contains("pickle-smoke:btn")) {
          Log.Error(
              $"pickle: tag click smoke failed - error message missing known tag 'pickle-smoke:btn': {missEx.Message}");
          TagStore.SessionActive = false;
          Find.WindowStack.TryRemove(testWindow);
          return;
        }
      }

      Find.WindowStack.TryRemove(testWindow);
      TagStore.SessionActive = false;

      Log.Message("pickle: tag click smoke passed");
    } catch (Exception ex) {
      Log.Error($"pickle: tag click smoke failed with exception: {ex}");
      TagStore.SessionActive = false;
    }
  }
}
