using UnityEngine;
using Verse;

namespace Pickle.Input;

internal static class TagOverlay {
  public static bool Enabled { get; set; }

  internal static void DrawOverlay() {
    if (!Enabled) {
      return;
    }

    Text.Font = GameFont.Small;
    float x = 10f;
    float y = 10f;

    Widgets.Label(new Rect(x, y, 200f, 30f), $"Tags: {TagStore.KnownTags.Count}");
    y += 30f;

    foreach (string tag in TagStore.KnownTags) {
      bool hasDuplicate = TagStore.TryGet(tag, out Rect _, out bool duplicate);
      string label = duplicate ? $"{tag} (DUPLICATE)" : tag;
      Widgets.Label(new Rect(x, y, 300f, 30f), label);
      y += 30f;
    }

    Text.Font = GameFont.Small;
  }
}
