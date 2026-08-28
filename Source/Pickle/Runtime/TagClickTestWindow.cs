using UnityEngine;
using Verse;

namespace Pickle.Runtime;

internal class TagClickTestWindow : Window {
  internal static bool Clicked;

  public TagClickTestWindow() {
    doCloseX = true;
    doCloseButton = true;
    windowRect = new Rect(100, 100, 200, 60);
  }

  public override void DoWindowContents(Rect inRect) {
    Rect buttonRect = new Rect(10, 10, 180, 40);
    PickleUI.Tag("pickle-smoke:btn", buttonRect);
    if (Widgets.ButtonText(buttonRect, "Click Me")) {
      Clicked = true;
    }
  }
}
