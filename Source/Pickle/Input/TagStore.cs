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

    // Unclip a window-local rect into game-window GUI space. Rects tagged inside windows or
    // groups are local, and every consumer wants GUI space: InputBackends.ToScreen applies
    // Prefs.UIScale itself, and hit tests compare against UI.MousePositionOnUIInverted.
    //
    // GUIToScreenRect cannot do it. Measured at 1920x1080 on one button drawn in one place: it
    // adds the clip origin UNSCALED and the local offset SCALED, so at 150% a window at GUI
    // y 375 holding a local y 285 came back as 802.5 - neither GUI space (660) nor screen space
    // (990). ToScreen then multiplied it by 1.5 again and sent the pointer to 1203 on a screen
    // 1080 tall. At scale 1 the two spaces coincide, which is why only a scaled interface ever
    // saw it.
    //
    // GUIToScreenPoint(zero) is exact for the origin: the scaled term vanishes at zero.
    Vector2 clipOrigin = GUIUtility.GUIToScreenPoint(Vector2.zero);
    Rect guiRect = new(rect.position + clipOrigin, rect.size);

    if (Store.TryGetValue(tag, out TagEntry? entry)) {
      entry.Duplicate = true;
      entry.DuplicateRect = guiRect;
      return;
    }

    Store[tag] = new TagEntry { Rect = guiRect, Duplicate = false, UiScale = Prefs.UIScale };
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
