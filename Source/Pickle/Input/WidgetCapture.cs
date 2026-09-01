using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Input;

/// <summary>
/// Records Widgets.ButtonText labels button labels into TagStore
/// with a "btn:" prefix, enabling vanilla button clicks by label with zero tagging.
/// </summary>
internal static class WidgetCapture {
  public static void AfterButtonText(Rect rect, string label) {
    if (string.IsNullOrEmpty(label)) {
      return;
    }

    TagStore.Record($"btn:{label}", rect);
  }
}
