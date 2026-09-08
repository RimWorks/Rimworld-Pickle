using RimWorks.Pickle.Input;
using UnityEngine;

namespace RimWorks.Pickle;

/// <summary>The one call a mod's own <c>OnGUI</c> makes to opt into Pickle: tagging a rect so a
/// scenario can find and click it.</summary>
public static class PickleUI {
  /// <summary>Records where a rect drew this frame under a tag, for <see cref="PickleContext.Click"/>
  /// and <see cref="PickleContext.Hover"/> to resolve. A no-op outside an active Pickle session.</summary>
  /// <param name="id">The tag a scenario refers to this rect by.</param>
  /// <param name="rect">The rect as drawn, in the caller's local GUI space.</param>
  public static void Tag(string id, Rect rect) {
    TagStore.Record(id, rect);
  }
}
