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
    // Rects tagged inside windows/groups are local; a backend expects game-window coords.
    Rect screenRect = GUIUtility.GUIToScreenRect(rect);

    if (Store.TryGetValue(tag, out TagEntry? entry)) {
      entry.Duplicate = true;
      entry.DuplicateRect = screenRect;
      return;
    }

    Store[tag] = new TagEntry { Rect = screenRect, Duplicate = false, UiScale = Prefs.UIScale };
  }

  public static void BeginFrame() {
    Store.Clear();
  }

  public static bool TryGet(string tag, out Rect rect, out bool duplicate) {
    // A rect recorded before the interface scale changed describes a layout that no longer
    // exists, and scaling it to screen space aims the pointer off the window. Treating it as
    // absent makes the caller wait for the frame that redraws the widget at the new scale.
    if (Store.TryGetValue(tag, out TagEntry? entry) && Mathf.Approximately(entry.UiScale, Prefs.UIScale)) {
      rect = entry.Rect;
      duplicate = entry.Duplicate;
      return true;
    }

    rect = default;
    duplicate = false;
    return false;
  }

  /// <summary>Whether the tag is held, but from a frame drawn at another interface scale.</summary>
  /// <param name="tag">The tag to look for.</param>
  /// <returns>True when a stale-scale entry is the only thing the store holds for that tag.</returns>
  public static bool HeldAtAnotherScale(string tag) {
    return Store.TryGetValue(tag, out TagEntry? entry) && !Mathf.Approximately(entry.UiScale, Prefs.UIScale);
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

  /// <summary>The interface scale the rect was measured at.</summary>
  public float UiScale { get; set; }

  public bool Duplicate { get; set; }

  public Rect DuplicateRect { get; set; }
}
