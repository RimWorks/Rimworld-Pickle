using UnityEngine;
using Verse;

namespace Pickle.Runtime;

internal class TagClickTestWindow : Window {
  public TagClickTestWindow() {
    doCloseX = true;
    doCloseButton = true;
    windowRect = new Rect(100, 100, 200, 60);
  }

  internal static bool Clicked { get; set; }

  public override void DoWindowContents(Rect inRect) {
    Rect buttonRect = new Rect(10, 10, 180, 40);
    PickleUI.Tag("pickle-smoke:btn", buttonRect);
    if (Widgets.ButtonText(buttonRect, "Click Me")) {
      Clicked = true;
    }
  }
}
