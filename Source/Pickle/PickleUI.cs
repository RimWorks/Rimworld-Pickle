using Pickle.Input;
using UnityEngine;

namespace Pickle;

public static class PickleUI {
  public static void Tag(string id, Rect rect) {
    TagStore.Record(id, rect);
  }
}
