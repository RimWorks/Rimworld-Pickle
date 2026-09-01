using RimWorks.Pickle.Input;
using UnityEngine;

namespace RimWorks.Pickle;

public static class PickleUI {
  public static void Tag(string id, Rect rect) {
    TagStore.Record(id, rect);
  }
}
