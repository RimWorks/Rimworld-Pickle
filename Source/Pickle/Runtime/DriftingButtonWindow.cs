using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Runtime;

/// <summary>A window whose button slides toward its resting place for a number of frames after the
/// window opens, then stays there. Stands in for a layout that is still settling.</summary>
internal class DriftingButtonWindow : Window {
  internal const string Label = "Drifting";

  private const float SlidePerFrame = 60f;
  private const float ButtonWidth = 120f;

  private readonly int driftFrames;
  private readonly int openedFrame = Time.frameCount;

  public DriftingButtonWindow(int driftFrames) {
    this.driftFrames = driftFrames;
    doCloseX = false;
    doCloseButton = false;
    layer = WindowLayer.Dialog;
    windowRect = new Rect(60f, 200f, 1500f, 140f);
  }

  internal static bool Clicked { get; set; }

  public override void DoWindowContents(Rect inRect) {
    int framesLeft = Mathf.Max(0, driftFrames - (Time.frameCount - openedFrame));
    Rect buttonRect = new Rect(10f + (framesLeft * SlidePerFrame), 10f, ButtonWidth, 40f);

    if (Widgets.ButtonText(buttonRect, Label)) {
      Clicked = true;
    }
  }
}
