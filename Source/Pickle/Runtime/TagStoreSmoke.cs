using System;
using System.Linq;
using System.Threading.Tasks;
using Pickle.Input;
using Pickle.Patching;
using UnityEngine;
using Verse;

namespace Pickle.Runtime;

internal static class TagStoreSmoke {
  private const string OneTag = "pickle-smoke:one";
  private const string DupTag = "pickle-smoke:dup";

  public static void DrawTagsPrefix() {
    PickleUI.Tag(OneTag, new Rect(10, 10, 50, 50));
    PickleUI.Tag(DupTag, new Rect(70, 10, 50, 50));
    PickleUI.Tag(DupTag, new Rect(130, 10, 50, 50));
  }

  internal static async Task Run() {
    try {
      PickleDriver bootDriver = PickleDriver.Instance;
      await bootDriver.WaitFrames(1);

      TagStore.SessionActive = true;

      PickleHooks.DuringUIRootOnGUI = DrawTagsPrefix;

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitFrames(3);

      PickleHooks.DuringUIRootOnGUI = null;

      bool oneExists = TagStore.TryGet(OneTag, out Rect _, out bool oneDuplicate);
      bool dupExists = TagStore.TryGet(DupTag, out Rect _, out bool dupDuplicate);
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

      if (!TagStore.KnownTags.Any(t => t == OneTag) || !TagStore.KnownTags.Any(t => t == DupTag)) {
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
}
