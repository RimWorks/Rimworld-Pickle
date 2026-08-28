using System;
using System.Linq;
using Pickle.Input;
using Pickle.Patching;
using UnityEngine;
using Verse;

namespace Pickle.Runtime;

internal static class TagStoreSmoke {
  internal static async void Run() {
    try {
      PickleDriver bootDriver = PickleDriver.Instance;
      await bootDriver.WaitFrames(1);

      TagStore.SessionActive = true;

      PickleHooks.DuringUIRootOnGUI = DrawTagsPrefix;

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitFrames(3);

      PickleHooks.DuringUIRootOnGUI = null;

      bool oneExists = TagStore.TryGet("pickle-smoke:one", out Rect oneRect, out bool oneDuplicate);
      bool dupExists = TagStore.TryGet("pickle-smoke:dup", out Rect _, out bool dupDuplicate);
      bool unknownExists = TagStore.TryGet("pickle-smoke:unknown", out Rect _, out bool _);

      if (!oneExists) {
        Log.Error("pickle: tag store smoke failed - pickle-smoke:one not found");
        return;
      }

      if (oneDuplicate) {
        Log.Error("pickle: tag store smoke failed - pickle-smoke:one marked as duplicate but should not be");
        return;
      }

      if (!dupExists) {
        Log.Error("pickle: tag store smoke failed - pickle-smoke:dup not found");
        return;
      }

      if (!dupDuplicate) {
        Log.Error("pickle: tag store smoke failed - pickle-smoke:dup not marked as duplicate");
        return;
      }

      if (unknownExists) {
        Log.Error("pickle: tag store smoke failed - unknown tag should not exist");
        return;
      }

      if (!TagStore.KnownTags.Any(t => t == "pickle-smoke:one") || !TagStore.KnownTags.Any(t => t == "pickle-smoke:dup")) {
        string knownTagsList = string.Join(", ", TagStore.KnownTags);
        Log.Error($"pickle: tag store smoke failed - KnownTags missing expected tags: {knownTagsList}");
        return;
      }

      TagStore.SessionActive = false;
      TagStore.BeginFrame();
      PickleUI.Tag("pickle-smoke:noop-test", new Rect(0, 0, 10, 10));
      if (TagStore.TryGet("pickle-smoke:noop-test", out Rect _, out bool _)) {
        Log.Error("pickle: tag store smoke failed - tag recorded when SessionActive is false");
        return;
      }

      Log.Message("pickle: tag store smoke passed");
    } catch (Exception ex) {
      Log.Error($"pickle: tag store smoke failed with exception: {ex}");
    }
  }

  public static void DrawTagsPrefix() {
    PickleUI.Tag("pickle-smoke:one", new Rect(10, 10, 50, 50));
    PickleUI.Tag("pickle-smoke:dup", new Rect(70, 10, 50, 50));
    PickleUI.Tag("pickle-smoke:dup", new Rect(130, 10, 50, 50));
  }
}
