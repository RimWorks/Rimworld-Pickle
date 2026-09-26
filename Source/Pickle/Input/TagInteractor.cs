using System;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Input;

internal static class TagInteractor {
  internal static bool TryResolve(string tag, out Rect rect, out string? error) {
    if (!TagStore.TryGet(tag, out Rect foundRect, out bool duplicate)) {
      rect = default;
      error = DescribeMiss(tag);
      return false;
    }

    if (duplicate) {
      rect = default;
      TagStore.TryGetDuplicate(tag, out Rect dupRect);
      string[] knownTags = [.. TagStore.KnownTags.OrderBy(t => t)];
      string tagList = knownTags.Length == 0 ? "no tags recorded this frame" : string.Join(", ", knownTags);
      error = $"tag '{tag}' is ambiguous; appeared at {foundRect} and {dupRect}; known tags: {tagList}";
      return false;
    }

    rect = foundRect;
    error = null;
    return true;
  }

  internal static string DescribeMiss(string tag) {
    if (TagStore.HeldAtAnotherScale(tag)) {
      return $"tag '{tag}' was last drawn at another interface scale, and nothing redrew it at "
          + $"{Prefs.UIScale:0.##}. Let a frame pass after changing the scale, so the widget is "
          + "measured where it now sits.";
    }

    string[] knownTags = [.. TagStore.KnownTags.OrderBy(t => t)];
    string tagList = knownTags.Length == 0 ? "no tags recorded this frame" : string.Join(", ", knownTags);
    return $"tag '{tag}' not found; known tags: {tagList}";
  }
}
