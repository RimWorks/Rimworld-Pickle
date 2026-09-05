using System.Collections.Generic;
using System.IO;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.Web;
using RimWorld;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.UI;

public static class RunnerToolbar {
  public const float ActionsWidth = 246f;
  private const float Padding = 8f;
  private const float ButtonHeight = 30f;

  public static float HeaderHeight(float width) => width < 900f ? 84f : 56f;

  public static float Height(float width) => width < 760f ? 82f : 48f;

  public static void DrawHeader(Rect rect, RunnerWindow window) {
    float x = rect.x + Padding;
    foreach ((string key, string label) in new[] { ("run", "Run"), ("fixtures", "Fixtures"), ("reports", "Reports") }) {
      Rect tab = new Rect(x, rect.y, 90f, 48f);
      bool active = window.Workspace == key;
      Label(tab, label, active ? RunnerStatusColors.Accent : Color.white, GameFont.Small);
      if (active) {
        Widgets.DrawBoxSolid(new Rect(tab.x, tab.yMax - 2f, tab.width, 2f), RunnerStatusColors.Accent);
      }

      if (Widgets.ButtonInvisible(tab)) {
        window.Workspace = key;
      }

      x += 94f;
    }

    float summaryY = rect.width < 900f ? rect.y + 48f : rect.y + 8f;
    float summaryX = rect.width < 900f ? rect.x + Padding : x + 18f;
    float summaryWidth = rect.xMax - summaryX - Padding;
    bool paused = window.ActiveSession?.IsPaused == true;
    string state = paused ? "Paused" : window.IsRunning ? "Running" : window.FailedResultsCount > 0 ? "Failed" : "Idle";
    if (!paused && window.ActiveSession?.PauseRequested == true) {
      state = "Pausing after current step";
    }

    Color color = StatusColor(window);
    RunnerStatusColors.DrawDot(new Vector2(summaryX + 4f, summaryY + 16f), color, 7f);
    float statusWidth = Mathf.Max(100f, summaryWidth - 320f);
    Label(new Rect(summaryX + 16f, summaryY, statusWidth - 16f, 18f), state, color, GameFont.Tiny);
    string detail = window.IsRunning ? window.ActiveSession?.CurrentStepDisplay ?? string.Empty
        : $"{window.ParsedFeaturesCount} features" + (window.LastRunAt.HasValue ? $" · last run {window.LastRunAt:HH:mm:ss}" : string.Empty);
    Label(new Rect(summaryX + 16f, summaryY + 18f, statusWidth - 16f, 18f), detail, RunnerStatusColors.Muted, GameFont.Tiny);
    Label(new Rect(summaryX + statusWidth, summaryY + 8f, 310f, 22f),
        $"{window.PassedResultsCount} passed · {window.FailedResultsCount} failed · {window.SkippedResultsCount} skipped", Color.white, GameFont.Tiny);
    Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width, Widgets.SeparatorLineColor);
  }

  public static void Draw(Rect rect, RunnerWindow window) {
    bool idle = !window.IsRunning && !FixtureCommands.IsBusy;
    float y = rect.y + 9f;
    Label(new Rect(rect.x + Padding, y, 42f, ButtonHeight), "Scope", RunnerStatusColors.Muted, GameFont.Tiny);
    Rect scope = new Rect(rect.x + 50f, y, 142f, ButtonHeight);
    string scopeLabel = window.RunScope == "selected" ? $"{window.SelectedScenarioCount} selected"
        : window.RunScope == "failed" ? $"{window.FailedResultsCount} failed" : $"All {window.TotalScenarioCount}";
    GUI.enabled = idle;
    if (Widgets.ButtonText(scope, scopeLabel)) {
      List<FloatMenuOption> options = [];
      foreach ((string key, string label) in new[] {
          ("all", $"All {window.TotalScenarioCount}"), ("selected", $"{window.SelectedScenarioCount} selected"), ("failed", $"{window.FailedResultsCount} failed") }) {
        options.Add(new FloatMenuOption(label, () => {
          window.RunScope = key;
          window.PublishSnapshot();
        }));
      }

      Find.WindowStack.Add(new FloatMenu(options));
    }

    Label(new Rect(scope.xMax + 22f, y, 40f, ButtonHeight), "Mode", RunnerStatusColors.Muted, GameFont.Tiny);
    Rect mode = new Rect(scope.xMax + 64f, y, 112f, ButtonHeight);
    DrawModeToggle(mode);
    Rect settings = new Rect(mode.xMax + 10f, y, 88f, ButtonHeight);
    if (Widgets.ButtonText(settings, "Options")) {
      OpenOptions(window);
    }

    float bulkY = rect.width < 760f ? y + 34f : y;
    float bulkX = rect.xMax - 258f;
    Label(new Rect(bulkX, bulkY, 100f, ButtonHeight), $"{window.SelectedScenarioCount} selected", RunnerStatusColors.Muted, GameFont.Tiny);
    if (Widgets.ButtonText(new Rect(bulkX + 106f, bulkY, 70f, ButtonHeight), $"All {window.TotalScenarioCount}")) {
      _ = RunnerCommands.SelectAll(true);
    }

    if (Widgets.ButtonText(new Rect(bulkX + 182f, bulkY, 70f, ButtonHeight), "Clear all")) {
      _ = RunnerCommands.SelectAll(false);
    }

    GUI.enabled = true;
  }

  public static void DrawActions(Rect rect, RunnerWindow window) {
    bool paused = window.ActiveSession?.IsPaused == true;
    bool stopping = window.ActiveSession?.CancelRequested == true;
    bool idle = !window.IsRunning && !FixtureCommands.IsBusy;
    float x = rect.xMax - ActionsWidth;
    GUI.enabled = window.IsRunning;
    bool follow = window.FollowRun;
    Widgets.CheckboxLabeled(new Rect(x, rect.y, 120f, ButtonHeight), "Follow run", ref follow);
    if (follow != window.FollowRun) {
      window.FollowRun = follow;
      window.PublishSnapshot();
    }

    x += 138f;
    int count = window.RunScope == "selected" ? window.SelectedScenarioCount
        : window.RunScope == "failed" ? window.FailedResultsCount : window.TotalScenarioCount;
    GUI.enabled = !stopping && (paused || (idle && count > 0));
    if (IconButton(new Rect(x, rect.y, 32f, ButtonHeight), paused ? "continue" : "run", paused ? "Continue run" : $"Run {count} scenarios", RunnerStatusColors.Accent)) {
      if (paused) {
        window.ContinueRun();
      } else {
        _ = RunnerCommands.Run(window.RunScope);
      }
    }

    x += 36f;
    GUI.enabled = window.IsRunning && !paused && !stopping && window.ActiveSession?.PauseRequested == false;
    if (IconButton(new Rect(x, rect.y, 32f, ButtonHeight), "pause", "Pause after current step", Color.white)) {
      window.ActiveSession?.RequestPause();
    }

    x += 36f;
    GUI.enabled = window.IsRunning && !stopping;
    if (IconButton(new Rect(x, rect.y, 32f, ButtonHeight), "abort", stopping ? "Aborting run" : "Abort run", RunnerStatusColors.FailedText)) {
      window.ActiveSession?.RequestCancel();
      window.PublishSnapshot();
    }

    GUI.enabled = true;
  }

  public static void DrawProgress(Rect rect, RunnerWindow window) {
    Widgets.DrawBoxSolid(rect, Widgets.SeparatorLineColor);
    float fraction = window.RunScenarioCount == 0 ? 0f : (float)window.CompletedScenarioCount / window.RunScenarioCount;
    Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), StatusColor(window));
  }

  public static void DrawReports(Rect rect, RunnerWindow window) {
    Label(new Rect(rect.x, rect.y, rect.width, 30f), "Last run report", Color.white, GameFont.Medium);
    Label(new Rect(rect.x, rect.y + 38f, rect.width, 24f), window.LastRunAt?.ToString("g") ?? "No completed run yet.", RunnerStatusColors.Muted, GameFont.Small);
    string report = Path.Combine(ScreenshotCapture.ReportRoot(), "report.html");
    GUI.enabled = File.Exists(report);
    if (Widgets.ButtonText(new Rect(rect.x, rect.y + 76f, 140f, 30f), "Pickle_OpenReport".Translate())) {
      Application.OpenURL(new System.Uri(report).AbsoluteUri);
    }

    GUI.enabled = true;
    if (Widgets.ButtonText(new Rect(rect.x + 150f, rect.y + 76f, 180f, 30f), "Pickle_OpenReportDir".Translate())) {
      OpenReportDirectory();
    }
  }

  private static Color StatusColor(RunnerWindow window) {
    return window.ActiveSession?.IsPaused == true ? RunnerStatusColors.Paused
        : window.IsRunning ? RunnerStatusColors.Accent
        : window.FailedResultsCount > 0 ? RunnerStatusColors.FailedText : RunnerStatusColors.Passed;
  }

  private static void Label(Rect rect, string label, Color color, GameFont font) {
    Text.Font = font;
    Text.Anchor = TextAnchor.MiddleLeft;
    GUI.color = color;
    Widgets.Label(rect, label.Truncate(rect.width));
    GUI.color = Color.white;
    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;
  }

  private static bool IconButton(Rect rect, string icon, string tooltip, Color color) {
    TooltipHandler.TipRegion(rect, tooltip);
    bool clicked = Widgets.ButtonText(rect, string.Empty);
    Texture2D texture = icon == "abort" ? TexButton.Stop : icon == "pause" ? TexButton.SpeedButtonTextures[0] : TexButton.Play;
    GUI.color = GUI.enabled ? color : new Color(color.r, color.g, color.b, 0.35f);
    GUI.DrawTexture(rect.ContractedBy(6f), texture, ScaleMode.ScaleToFit);
    GUI.color = Color.white;
    return clicked;
  }

  private static void OpenOptions(RunnerWindow window) {
    Find.WindowStack.Add(new FloatMenu([
        new FloatMenuOption($"Pause on failure: {(BreakOnFailureState.Enabled ? "On" : "Off")}", () => {
          BreakOnFailureState.Enabled = !BreakOnFailureState.Enabled;
          window.PublishSnapshot();
        }),
        new FloatMenuOption($"Include @wip: {(IncludeWipState.Enabled ? "On" : "Off")}", () => {
          IncludeWipState.Enabled = !IncludeWipState.Enabled;
          window.PublishSnapshot();
        }),
        new FloatMenuOption($"Show run pill: {(RunPillState.Enabled ? "On" : "Off")}", () => {
          RunPillState.Enabled = !RunPillState.Enabled;
          window.PublishSnapshot();
        }),
    ]));
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
