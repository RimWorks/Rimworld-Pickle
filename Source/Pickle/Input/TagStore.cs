using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Input;

internal static class TagStore {
  private static readonly Dictionary<string, TagEntry> Store = [];

  // Written from the smoke/session bootstrap, read every frame from OnGUI on the main
  // thread. volatile so the main thread cannot cache a stale false and silently no-op.
  private static volatile bool sessionActive;

  public static bool SessionActive {
    get => sessionActive;
    set => sessionActive = value;
  }

  public static IReadOnlyCollection<string> KnownTags => Store.Keys;

  public static void Record(string tag, Rect rect) {
    if (!sessionActive) {
      return;
    }

    // OnGUI runs several times per frame, so recording only on Repaint keeps one entry
    // per tag. Duplicate then means two rects claimed the tag, not one seen twice.
    if (Event.current == null || Event.current.type != EventType.Repaint) {
      return;
    }

    // Convert window-local rect to game-window space using the active GUI group matrix.
    // Rects tagged inside windows/groups are local; XdoInput expects game-window coords.
    Rect screenRect = GUIUtility.GUIToScreenRect(rect);

    if (Store.TryGetValue(tag, out TagEntry? entry)) {
      entry.Duplicate = true;
      entry.DuplicateRect = screenRect;
      return;
    }

    Store[tag] = new TagEntry { Rect = screenRect, Duplicate = false };
  }

  public static void BeginFrame() {
    Store.Clear();
  }

  public static bool TryGet(string tag, out Rect rect, out bool duplicate) {
    if (Store.TryGetValue(tag, out TagEntry? entry)) {
      rect = entry.Rect;
      duplicate = entry.Duplicate;
      return true;
    }

    rect = default;
    duplicate = false;
    return false;
  }

  public static bool TryGetDuplicate(string tag, out Rect duplicateRect) {
    if (Store.TryGetValue(tag, out TagEntry? entry) && entry.Duplicate) {
      duplicateRect = entry.DuplicateRect;
      return true;
    }

    duplicateRect = default;
    return false;
  }
}

internal class TagEntry {
  public Rect Rect { get; set; }

  public bool Duplicate { get; set; }

  public Rect DuplicateRect { get; set; }
}
