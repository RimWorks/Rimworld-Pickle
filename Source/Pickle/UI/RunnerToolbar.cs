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

/// <summary>Draws the runner window's top strip: workspace tabs, the run status summary, scope and mode
/// controls, action buttons, and the reports panel.</summary>
public static class RunnerToolbar {
  /// <summary>How wide the action button cluster (follow, run, pause, abort) reserves at the header's right edge.</summary>
  public const float ActionsWidth = 246f;
  private const float Padding = 8f;
  private const float ButtonHeight = 30f;

  /// <summary>The header row's height, taller below a width where the summary wraps to a second line.</summary>
  /// <param name="width">The available header width.</param>
  /// <returns>The header height in pixels.</returns>
  public static float HeaderHeight(float width) => width < 900f ? 84f : 56f;

  /// <summary>The scope and mode control row's height, taller below a width where the bulk-select controls wrap.</summary>
  /// <param name="width">The available row width.</param>
  /// <returns>The row height in pixels.</returns>
  public static float Height(float width) => width < 760f ? 82f : 48f;

  /// <summary>Draws the workspace tabs and the run status summary, with a separator line beneath.</summary>
  /// <param name="rect">The area to draw the header in.</param>
  /// <param name="window">The runner window supplying tab and run state.</param>
  public static void DrawHeader(Rect rect, RunnerWindow window) {
    float x = WorkspaceTabs(rect, window);
    Summary(rect, window, x);
    Widgets.DrawLineHorizontal(rect.x, rect.yMax, rect.width, Widgets.SeparatorLineColor);
  }

  /// <summary>Draws the run scope picker, the watch/fast mode toggle, the options menu button, and the bulk select controls.</summary>
  /// <param name="rect">The area to draw the row in.</param>
  /// <param name="window">The runner window supplying and receiving the control state.</param>
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

  /// <summary>Draws the follow toggle and the run, pause, and abort buttons.</summary>
  /// <param name="rect">The area to draw the buttons in; they align to its right edge.</param>
  /// <param name="window">The runner window the buttons act on.</param>
  public static void DrawActions(Rect rect, RunnerWindow window) {
    float x = rect.xMax - ActionsWidth;
    FollowToggle(new Rect(x, rect.y, 120f, ButtonHeight), window);
    RunButton(new Rect(x + 138f, rect.y, 32f, ButtonHeight), window);
    PauseButton(new Rect(x + 174f, rect.y, 32f, ButtonHeight), window);
    AbortButton(new Rect(x + 210f, rect.y, 32f, ButtonHeight), window);
    GUI.enabled = true;
  }

  /// <summary>Draws the run progress bar, filled by completed over total scenarios and colored by run status.</summary>
  /// <param name="rect">The area to draw the bar in.</param>
  /// <param name="window">The runner window supplying progress and status.</param>
  public static void DrawProgress(Rect rect, RunnerWindow window) {
    Widgets.DrawBoxSolid(rect, Widgets.SeparatorLineColor);
    float fraction = window.RunScenarioCount == 0 ? 0f : (float)window.CompletedScenarioCount / window.RunScenarioCount;
    Widgets.DrawBoxSolid(new Rect(rect.x, rect.y, rect.width * Mathf.Clamp01(fraction), rect.height), StatusColor(window));
  }

  /// <summary>Draws the last run's timestamp and buttons to open the report file or its directory.</summary>
  /// <param name="rect">The area to draw the panel in.</param>
  /// <param name="window">The runner window supplying the last run's timestamp.</param>
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

  private static float WorkspaceTabs(Rect rect, RunnerWindow window) {
    float x = rect.x + Padding;
    foreach ((string key, string label) in new[] { ("run", "Run"), ("fixtures", "Fixtures"), ("reports", "Reports") }) {
      Rect tab = new Rect(x, rect.y, 90f, 48f);
      bool active = window.Workspace == key;
      Label(tab, label, active ? RunnerStatusColors.Accent : Color.white, GameFont.Small, TextAnchor.MiddleCenter);
      if (active) {
        Widgets.DrawBoxSolid(new Rect(tab.x, tab.yMax - 2f, tab.width, 2f), RunnerStatusColors.Accent);
      }

      if (Widgets.ButtonInvisible(tab)) {
        window.Workspace = key;
      }

      x += 94f;
    }

    return x;
  }

  private static void Summary(Rect rect, RunnerWindow window, float tabsEnd) {
    float summaryY = rect.width < 900f ? rect.y + 48f : rect.y + 8f;
    float summaryX = rect.width < 900f ? rect.x + Padding : tabsEnd + 18f;
    float summaryWidth = rect.xMax - summaryX - Padding;

    Color color = StatusColor(window);
    RunnerStatusColors.DrawDot(new Vector2(summaryX + 4f, summaryY + 16f), color, 7f);
    float statusWidth = Mathf.Max(100f, summaryWidth - 320f);
    Label(new Rect(summaryX + 16f, summaryY, statusWidth - 16f, 18f), StateLabel(window), color, GameFont.Tiny);
    string detail = window.IsRunning ? window.ActiveSession?.CurrentStepDisplay ?? string.Empty
        : $"{window.ParsedFeaturesCount} features" + (window.LastRunAt.HasValue ? $" · last run {window.LastRunAt:HH:mm:ss}" : string.Empty);
    Label(new Rect(summaryX + 16f, summaryY + 18f, statusWidth - 16f, 18f), detail, RunnerStatusColors.Muted, GameFont.Tiny);
    Label(new Rect(summaryX + statusWidth, summaryY + 8f, summaryWidth - statusWidth, 22f),
        Counts(window), Color.white, GameFont.Tiny, TextAnchor.MiddleRight);
  }

  // Rich text rather than four rects: the colours have to survive Truncate collapsing the
  // line when the header is narrow.
  private static string Counts(RunnerWindow window) {
    int passed = window.PassedResultsCount;
    int failed = window.FailedResultsCount;
    int skipped = window.SkippedResultsCount;
    int notRun = Mathf.Max(0, window.TotalScenarioCount - passed - failed - skipped);
    string pass = ColorUtility.ToHtmlStringRGB(RunnerStatusColors.Passed);
    string fail = ColorUtility.ToHtmlStringRGB(RunnerStatusColors.FailedText);
    return $"<color=#{pass}>{passed} passed</color> · <color=#{fail}>{failed} failed</color> · {skipped} skipped · {notRun} not run";
  }

  private static string StateLabel(RunnerWindow window) {
    bool paused = window.ActiveSession?.IsPaused == true;
    if (!paused && window.ActiveSession?.PauseRequested == true) {
      return "Pausing after current step";
    }

    return paused ? "Paused" : window.IsRunning ? "Running" : window.FailedResultsCount > 0 ? "Failed" : "Idle";
  }

  private static void FollowToggle(Rect rect, RunnerWindow window) {
    GUI.enabled = window.IsRunning;
    bool follow = window.FollowRun;
    Widgets.CheckboxLabeled(rect, "Follow run", ref follow);
    if (follow != window.FollowRun) {
      window.FollowRun = follow;
      window.PublishSnapshot();
    }
  }

  private static void RunButton(Rect rect, RunnerWindow window) {
    bool paused = window.ActiveSession?.IsPaused == true;
    bool stopping = window.ActiveSession?.CancelRequested == true;
    bool idle = !window.IsRunning && !FixtureCommands.IsBusy;
    int count = window.RunScope == "selected" ? window.SelectedScenarioCount
        : window.RunScope == "failed" ? window.FailedResultsCount : window.TotalScenarioCount;

    GUI.enabled = !stopping && (paused || (idle && count > 0));
    if (!IconButton(rect, paused ? "continue" : "run", paused ? "Continue run" : $"Run {count} scenarios", RunnerStatusColors.Accent)) {
      return;
    }

    if (paused) {
      window.ContinueRun();
    } else {
      _ = RunnerCommands.Run(window.RunScope);
    }
  }

  private static void PauseButton(Rect rect, RunnerWindow window) {
    bool paused = window.ActiveSession?.IsPaused == true;
    bool stopping = window.ActiveSession?.CancelRequested == true;
    GUI.enabled = window.IsRunning && !paused && !stopping && window.ActiveSession?.PauseRequested == false;
    if (IconButton(rect, "pause", "Pause after current step", Color.white)) {
      window.ActiveSession?.RequestPause();
    }
  }

  private static void AbortButton(Rect rect, RunnerWindow window) {
    bool stopping = window.ActiveSession?.CancelRequested == true;
    GUI.enabled = window.IsRunning && !stopping;
    if (IconButton(rect, "abort", stopping ? "Aborting run" : "Abort run", RunnerStatusColors.FailedText)) {
      window.ActiveSession?.RequestCancel();
      window.PublishSnapshot();
    }
  }

  private static Color StatusColor(RunnerWindow window) {
    return window.ActiveSession?.IsPaused == true ? RunnerStatusColors.Paused
        : window.IsRunning ? RunnerStatusColors.Accent
        : window.FailedResultsCount > 0 ? RunnerStatusColors.FailedText : RunnerStatusColors.Passed;
  }

  private static void Label(Rect rect, string label, Color color, GameFont font) {
    Label(rect, label, color, font, TextAnchor.MiddleLeft);
  }

  private static void Label(Rect rect, string label, Color color, GameFont font, TextAnchor anchor) {
    Text.Font = font;
    Text.Anchor = anchor;
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
      Log.WarnTo("Pickle", "could not open report dir {Dir}: {Error}", [dir, ex.Message]);
    }

    Messages.Message("Pickle_ReportDirMessage".Translate(dir), MessageTypeDefOf.NeutralEvent, false);
  }
}
