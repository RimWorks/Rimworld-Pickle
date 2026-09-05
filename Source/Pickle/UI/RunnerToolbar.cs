using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Runtime;
using RimWorld;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Top toolbar: run controls, watch/fast mode, the break-on-failure and include-@wip
/// toggles, and the fixture manager / report actions on the right.
/// </summary>
public static class RunnerToolbar {
  private const float RowHeight = 34f;
  private const float Padding = 6f;
  private static readonly float[] Widths = [80f, 118f, 118f, 96f, 86f, 112f, 170f, 132f, 116f, 92f, 128f, 106f, 106f, 116f, 110f];

  public static float Height(float width) {
    Rect bounds = new Rect(0f, 0f, width, 0f);
    float x = Padding;
    float y = 0f;
    foreach (float controlWidth in Widths) {
      _ = Next(bounds, ref x, ref y, controlWidth);
    }

    return y + RowHeight;
  }

  public static void Draw(Rect rect, RunnerWindow window) {
    float x = rect.x + Padding;
    float y = rect.y;
    bool idle = !window.IsRunning && !Web.FixtureCommands.IsBusy;

    GUI.enabled = idle && window.TotalScenarioCount > 0;
    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[0]), "Pickle_RunAll".Translate())) {
      _ = window.RunAllAndWait();
    }

    GUI.enabled = idle && window.HasAnyScenarioSelected;
    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[1]), "Pickle_RunSelected".Translate())) {
      window.RunSelected();
    }

    GUI.enabled = idle && window.FailedResultsCount > 0;
    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[2]), "Pickle_RerunFailed".Translate())) {
      window.RerunFailed();
    }

    GUI.enabled = window.ActiveSession?.IsPausedForBreak == true;
    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[3]), "Pickle_ContinueRun".Translate())) {
      window.ContinueRun();
    }

    GUI.enabled = window.IsRunning && window.ActiveSession?.CancelRequested == false;
    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[4]), "Pickle_AbortRun".Translate())) {
      window.ActiveSession?.RequestCancel();
    }

    GUI.enabled = true;
    DrawModeToggle(Next(rect, ref x, ref y, Widths[5]));

    bool breakOn = BreakOnFailureState.Enabled;
    Widgets.CheckboxLabeled(Next(rect, ref x, ref y, Widths[6]), "Pickle_BreakOnFailure".Translate(), ref breakOn);
    if (breakOn != BreakOnFailureState.Enabled) {
      BreakOnFailureState.Enabled = breakOn;
      window.PublishSnapshot();
    }

    bool includeWip = IncludeWipState.Enabled;
    Widgets.CheckboxLabeled(Next(rect, ref x, ref y, Widths[7]), "Pickle_IncludeWip".Translate(), ref includeWip);
    if (includeWip != IncludeWipState.Enabled) {
      IncludeWipState.Enabled = includeWip;
      window.PublishSnapshot();
    }

    bool showPill = RunPillState.Enabled;
    Widgets.CheckboxLabeled(Next(rect, ref x, ref y, Widths[8]), "Pickle_ShowRunPill".Translate(), ref showPill);
    if (showPill != RunPillState.Enabled) {
      RunPillState.Enabled = showPill;
      window.PublishSnapshot();
    }

    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[9]), "Pickle_Fixtures".Translate())) {
      Find.WindowStack.Add(new FixtureManagerDialog());
    }

    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[10]), "Pickle_OpenReportDir".Translate())) {
      OpenReportDirectory();
    }

    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[11]), "Pickle_SelectAll".Translate())) {
      _ = Web.RunnerCommands.SelectAll(true);
    }

    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[12]), "Pickle_DeselectAll".Translate())) {
      _ = Web.RunnerCommands.SelectAll(false);
    }

    GUI.enabled = window.IsRunning && !window.FollowRun;
    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[13]), "Follow the run")) {
      window.FollowRun = true;
      window.PublishSnapshot();
    }

    GUI.enabled = true;
    string report = System.IO.Path.Combine(ScreenshotCapture.ReportRoot(), "report.html");
    GUI.enabled = System.IO.File.Exists(report);
    if (Widgets.ButtonText(Next(rect, ref x, ref y, Widths[14]), "Pickle_OpenReport".Translate())) {
      Application.OpenURL(new System.Uri(report).AbsoluteUri);
    }

    GUI.enabled = true;
  }

  private static Rect Next(Rect bounds, ref float x, ref float y, float width) {
    if (x + width > bounds.xMax - Padding && x > bounds.x + Padding) {
      x = bounds.x + Padding;
      y += RowHeight;
    }

    Rect control = new Rect(x, y + 3f, width, RowHeight - 6f);
    x += width + Padding;
    return control;
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

    DrawSegment(watchRect, "Pickle_ModeWatch".Translate(), watchActive, PickleRunMode.Mode.Watch);
    DrawSegment(fastRect, "Pickle_ModeFast".Translate(), !watchActive, PickleRunMode.Mode.Fast);
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
      RunnerWindow.Instance.PublishSnapshot();
    }
  }

  private static void OpenReportDirectory() {
    string dir = ScreenshotCapture.ReportRoot();
    try {
      System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo {
        FileName = dir,
        UseShellExecute = true,
      });
    } catch (System.Exception ex) {
      Log.Warn("pickle: could not open report dir {Dir}: {Error}", [dir, ex.Message]);
    }

    Messages.Message("Pickle_ReportDirMessage".Translate(dir), MessageTypeDefOf.NeutralEvent, false);
  }
}
