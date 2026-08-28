using Pickle.Evidence;
using Pickle.Runtime;
using RimWorld;
using UnityEngine;
using Verse;

namespace Pickle.UI;

/// <summary>
/// Top toolbar: run controls, watch/fast mode, break-on-failure toggle, and the
/// save-fixture / open-report-dir actions on the right.
/// </summary>
public static class RunnerToolbar {
  private const float ButtonWidth = 118f;
  private const float SegWidth = 56f;
  private const float Padding = 6f;

  public static void Draw(Rect rect, RunnerWindow window) {
    float x = rect.x + Padding;

    GUI.enabled = !window.IsRunning && window.HasAnyScenarioSelected;

    Rect runRect = new Rect(x, rect.y + 3f, ButtonWidth, rect.height - 6f);
    if (Widgets.ButtonText(runRect, "▶ Run selected")) {
      window.RunSelected();
    }

    GUI.enabled = !window.IsRunning;

    x += ButtonWidth + Padding;
    Rect rerunRect = new Rect(x, rect.y + 3f, ButtonWidth, rect.height - 6f);
    if (Widgets.ButtonText(rerunRect, "↻ Rerun failed")) {
      window.RerunFailed();
    }

    GUI.enabled = true;

    x += ButtonWidth + (Padding * 2f);
    Rect segRect = new Rect(x, rect.y + 3f, SegWidth * 2f, rect.height - 6f);
    DrawModeToggle(segRect);

    x += (SegWidth * 2f) + (Padding * 2f);
    Rect breakRect = new Rect(x, rect.y, 220f, rect.height);
    bool breakOn = BreakOnFailureState.Enabled;
    Widgets.CheckboxLabeled(breakRect, "Break on failure", ref breakOn);
    BreakOnFailureState.Enabled = breakOn;

    const float openWidth = 128f;
    const float saveWidth = 110f;
    Rect openRect = new Rect(rect.xMax - openWidth - Padding, rect.y + 3f, openWidth, rect.height - 6f);
    Rect saveRect = new Rect(openRect.x - saveWidth - Padding, rect.y + 3f, saveWidth, rect.height - 6f);

    if (Widgets.ButtonText(saveRect, "Save fixture…")) {
      Find.WindowStack.Add(new SaveFixtureDialog(window.DiscoveredSuites));
    }

    if (Widgets.ButtonText(openRect, "Open report dir")) {
      OpenReportDirectory();
    }
  }

  // A segmented control, not two buttons. DrawHighlightSelected alone reads as a smudge
  // rather than a selection.
  private static void DrawModeToggle(Rect rect) {
    Rect watchRect = new Rect(rect.x, rect.y, rect.width / 2f, rect.height);
    Rect fastRect = new Rect(rect.x + (rect.width / 2f), rect.y, rect.width / 2f, rect.height);
    bool watchActive = PickleRunMode.Current == PickleRunMode.Mode.Watch;

    Widgets.DrawBoxSolid(rect, RunnerStatusColors.SegmentTrough);
    Widgets.DrawBoxSolid(watchActive ? watchRect : fastRect, RunnerStatusColors.SegmentActive);

    Color previousBorder = GUI.color;
    GUI.color = RunnerStatusColors.SegmentBorder;
    Widgets.DrawBox(rect, 1, BaseContent.WhiteTex);
    GUI.color = previousBorder;

    DrawSegment(watchRect, "Watch", watchActive, PickleRunMode.Mode.Watch);
    DrawSegment(fastRect, "Fast", !watchActive, PickleRunMode.Mode.Fast);
  }

  private static void DrawSegment(Rect rect, string label, bool active, PickleRunMode.Mode mode) {
    if (Mouse.IsOver(rect) && !active) {
      Widgets.DrawHighlight(rect);
    }

    Color previous = GUI.color;
    GUI.color = active ? Color.white : RunnerStatusColors.Muted;
    Text.Anchor = TextAnchor.MiddleCenter;
    Widgets.Label(rect, label);
    Text.Anchor = TextAnchor.UpperLeft;
    GUI.color = previous;

    if (Widgets.ButtonInvisible(rect)) {
      PickleRunMode.Current = mode;
    }
  }

  private static void OpenReportDirectory() {
    string dir = ScreenshotCapture.ReportsDirectory();
    try {
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
        FileName = dir,
        UseShellExecute = true,
      });
    } catch (System.Exception ex) {
      Log.Warning($"pickle: could not open report dir {dir}: {ex.Message}");
    }

    Messages.Message($"Report dir: {dir}", MessageTypeDefOf.NeutralEvent, false);
  }
}
