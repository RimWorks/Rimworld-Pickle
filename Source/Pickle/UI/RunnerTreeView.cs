using System;
using System.Collections.Generic;
using System.Linq;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Ui;
using RimWorld;
using UnityEngine;
using Verse;
using Verse.Sound;

namespace RimWorks.Pickle.UI;

/// <summary>Left-pane tree with tri-state checkboxes that derive from their scenario children.
/// Indices are numbered as RunAsync numbers them, so a row matches its stored result.</summary>
public static class RunnerTreeView {
  private const float ModRowHeight = 26f;
  private const float FeatureRowHeight = 22f;
  private const float ScenarioRowHeight = 20f;
  private const float ModIndent = 22f;
  private const float FeatureIndent = 42f;
  private const float ScenarioIndent = 58f;
  private const float ArrowSize = 16f;
  private const float ModArrowX = 4f;
  private const float FeatureArrowX = 24f;
  private const float ModCheckboxSize = 24f;
  private const float FeatureCheckboxSize = 18f;
  private const float ScenarioCheckboxSize = 16f;

  // Rebuilt on Layout only. IMGUI replays DoWindowContents once per queued event, and
  // every pass of a frame has to see the same rows or control ids stop lining up.
  private static readonly List<Row> Rows = [];
  private static readonly List<float> RowTops = [];

  // GenText.Truncate measures the font per character on every miss, so each label
  // width gets its own cache; a pane resize drops all three.
  private static readonly Dictionary<string, string> ModTruncation = [];
  private static readonly Dictionary<string, string> FeatureTruncation = [];
  private static readonly Dictionary<string, string> ScenarioTruncation = [];

  private static float contentHeight;
  private static int visibleFirst;
  private static int visibleLast;
  private static int lastWidthKey = -1;
  private static (string SourcePath, int ScenarioIndex)? lastFollowed;

  private enum RowKind {
    Mod,
    Feature,
    Scenario,
  }

  public static void Draw(Rect outRect, RunnerWindow window) {
    if (Event.current.type == EventType.Layout) {
      RebuildRows(window);
      if (window.FollowRun && window.Selected != lastFollowed) {
        int selectedRow = Rows.FindIndex(row => row.Kind == RowKind.Scenario
            && window.Selected == (row.Plan?.SourcePath ?? string.Empty, row.Index));
        if (selectedRow >= 0) {
          float top = RowTops[selectedRow];
          float bottom = top + Rows[selectedRow].Height;
          float scrollY = Mathf.Clamp(window.TreeScroll.y, bottom - outRect.height, top);
          window.TreeScroll = new Vector2(0f, Mathf.Max(0f, scrollY));
        }

        lastFollowed = window.Selected;
      }
    }

    Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, contentHeight);

    // The truncation caches key on the string alone, so a new width invalidates them.
    int widthKey = Mathf.RoundToInt(viewRect.width);
    if (widthKey != lastWidthKey) {
      ModTruncation.Clear();
      FeatureTruncation.Clear();
      ScenarioTruncation.Clear();
      lastWidthKey = widthKey;
    }

    Vector2 scroll = window.TreeScroll;
    Widgets.BeginScrollView(outRect, ref scroll, viewRect);
    window.TreeScroll = scroll;

    // Rows are fixed-height single lines; word wrap would push overflow text
    // into the row below instead of clipping, corrupting the whole tree.
    Text.WordWrap = false;

    // Picked on Layout only. A range that moved mid-frame would change how many controls
    // draw, shifting the scrollbar's ids out from under an active drag.
    if (Event.current.type == EventType.Layout) {
      (visibleFirst, visibleLast) = RowRange.Visible(RowTops, contentHeight, scroll.y, outRect.height);
    }

    int first = Math.Min(visibleFirst, Rows.Count);
    int last = Math.Min(visibleLast, Rows.Count);
    for (int i = first; i < last; i++) {
      Row row = Rows[i];
      Rect rowRect = new Rect(0f, RowTops[i], viewRect.width, row.Height);
      switch (row.Kind) {
        case RowKind.Mod:
          DrawModRow(rowRect, window, row.ModName, row.ModScenarioCount, row.SelectionKeys!);
          break;
        case RowKind.Feature:
          DrawFeatureRow(rowRect, window, row.Plan!, row.Index, row.SelectionKeys!);
          break;
        default:
          DrawScenarioRow(rowRect, window, row.Plan!, row.Scenario!, row.Index);
          break;
      }
    }

    Text.WordWrap = true;
    Widgets.EndScrollView();
  }

  private static void RebuildRows(RunnerWindow window) {
    Rows.Clear();
    RowTops.Clear();
    float y = 0f;

    Dictionary<FeaturePlan, int> startIndices = ComputeStartIndices(window);
    foreach (IGrouping<string, (DiscoveredSuite Suite, FeaturePlan Plan)> group in window.ParsedFeatures.GroupBy(f => f.Suite.ModName)) {
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> features = [.. group.Where(f => FeatureHasVisibleScenario(window, f.Suite, f.Plan))];

      if (features.Count == 0) {
        continue;
      }

      // Keyed to what is on screen, so filtering to a tag and ticking the mod selects that
      // tag's scenarios and nothing hidden.
      List<(string SourcePath, int Index)> modKeys = [];
      List<List<(string SourcePath, int Index)>> featureKeys = [];
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in features) {
        string featurePath = plan.SourcePath ?? string.Empty;
        int featureStart = startIndices[plan];
        List<(string SourcePath, int Index)> keys = [];
        for (int i = 0; i < plan.Scenarios.Count; i++) {
          if (IsScenarioVisible(window, suite, plan, plan.Scenarios[i])) {
            keys.Add((featurePath, featureStart + i));
          }
        }

        featureKeys.Add(keys);
        modKeys.AddRange(keys);
      }

      y = AddRow(
          new Row {
            Kind = RowKind.Mod,
            Height = ModRowHeight,
            ModName = group.Key,
            ModScenarioCount = modKeys.Count,
            SelectionKeys = modKeys,
          },
          y);

      if (window.CollapsedMods.Contains(group.Key)) {
        continue;
      }

      for (int f = 0; f < features.Count; f++) {
        (DiscoveredSuite suite, FeaturePlan plan) = features[f];
        int startIndex = startIndices[plan];
        y = AddRow(
            new Row {
              Kind = RowKind.Feature,
              Height = FeatureRowHeight,
              Plan = plan,
              Index = startIndex,
              SelectionKeys = featureKeys[f],
            },
            y);

        if (window.CollapsedFeatures.Contains(plan.SourcePath ?? string.Empty)) {
          continue;
        }

        for (int i = 0; i < plan.Scenarios.Count; i++) {
          ScenarioPlan scenario = plan.Scenarios[i];
          if (!IsScenarioVisible(window, suite, plan, scenario)) {
            continue;
          }

          y = AddRow(new Row { Kind = RowKind.Scenario, Height = ScenarioRowHeight, Plan = plan, Scenario = scenario, Index = startIndex + i }, y);
        }
      }
    }

    contentHeight = y;
  }

  private static float AddRow(Row row, float y) {
    Rows.Add(row);
    RowTops.Add(y);
    return y + row.Height;
  }

  private static Dictionary<FeaturePlan, int> ComputeStartIndices(RunnerWindow window) {
    Dictionary<FeaturePlan, int> map = [];
    int running = 0;
    foreach ((DiscoveredSuite Suite, FeaturePlan Plan) entry in window.ParsedFeatures) {
      map[entry.Plan] = running;
      running += entry.Plan.Scenarios.Count;
    }

    return map;
  }

  private static bool FeatureHasVisibleScenario(RunnerWindow window, DiscoveredSuite suite, FeaturePlan plan) {
    return plan.Scenarios.Any(s => IsScenarioVisible(window, suite, plan, s));
  }

  private static bool IsScenarioVisible(RunnerWindow window, DiscoveredSuite suite, FeaturePlan plan, ScenarioPlan scenario) {
    return window.IsScenarioVisible(suite, plan, scenario);
  }

  private static void DrawModRow(
      Rect rect,
      RunnerWindow window,
      string modName,
      int scenarioCount,
      List<(string SourcePath, int Index)> keys) {
    Widgets.DrawHighlightIfMouseover(rect);

    bool toggled = DrawArrow(rect, ModArrowX, window.CollapsedMods.Contains(modName));

    MultiCheckboxState state = ComputeCheckState(window, keys);
    Rect checkRect = new Rect(rect.x + ModIndent, rect.y + ((rect.height - ModCheckboxSize) / 2f), ModCheckboxSize, ModCheckboxSize);
    MultiCheckboxState newState = CheckboxMultiIdFree(checkRect, state);
    if (newState != state) {
      SetSelected(window, keys, newState == MultiCheckboxState.On);
    }

    Rect labelRect = new Rect(checkRect.xMax + 6f, rect.y, rect.width - checkRect.xMax - 90f, rect.height);
    Text.Anchor = TextAnchor.MiddleLeft;
    Widgets.Label(labelRect, modName.Truncate(labelRect.width, ModTruncation));

    Text.Font = GameFont.Tiny;
    GUI.color = RunnerStatusColors.Muted;
    Rect countRect = new Rect(rect.xMax - 80f, rect.y, 76f, rect.height);
    Text.Anchor = TextAnchor.MiddleRight;
    Widgets.Label(countRect, $"{scenarioCount} scenarios");
    GUI.color = Color.white;
    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;

    // Last, so the checkbox and the arrow have already claimed their own clicks.
    if (toggled || ClickedIdFree(rect)) {
      Toggle(window.CollapsedMods, modName);
    }
  }

  private static void DrawFeatureRow(
      Rect rect, RunnerWindow window, FeaturePlan plan, int startIndex, List<(string SourcePath, int Index)> keys) {
    Widgets.DrawHighlightIfMouseover(rect);

    string featureKey = plan.SourcePath ?? string.Empty;
    bool toggled = DrawArrow(rect, FeatureArrowX, window.CollapsedFeatures.Contains(featureKey));

    MultiCheckboxState state = ComputeCheckState(window, keys);
    Rect checkRect = new Rect(rect.x + FeatureIndent, rect.y + ((rect.height - FeatureCheckboxSize) / 2f), FeatureCheckboxSize, FeatureCheckboxSize);
    MultiCheckboxState newState = CheckboxMultiIdFree(checkRect, state);
    if (newState != state) {
      SetSelected(window, keys, newState == MultiCheckboxState.On);
    }

    int total = plan.Scenarios.Count;
    int failed = 0;
    int ran = 0;
    for (int i = 0; i < total; i++) {
      if (window.TryGetResult(plan.SourcePath ?? string.Empty, startIndex + i, out ScenarioResult result)) {
        ran++;
        if (result.Outcome == ScenarioOutcome.Failed) {
          failed++;
        }
      }
    }

    float dotX = checkRect.xMax + 8f;
    Color dotColor = RollupColor(failed, ran);
    RunnerStatusColors.DrawDot(new Vector2(dotX, rect.y + (rect.height / 2f)), dotColor);

    string countText = RollupCount(failed, ran, total);

    float labelX = dotX + 12f;
    Rect labelRect = new Rect(labelX, rect.y, rect.width - (labelX - rect.x) - 100f, rect.height);
    Text.Anchor = TextAnchor.MiddleLeft;
    Widgets.Label(labelRect, plan.Name.Truncate(labelRect.width, FeatureTruncation));

    Text.Font = GameFont.Tiny;
    GUI.color = failed > 0 ? RunnerStatusColors.FailedText : RunnerStatusColors.Muted;
    Rect countRect = new Rect(rect.xMax - 90f, rect.y, 86f, rect.height);
    Text.Anchor = TextAnchor.MiddleRight;
    Widgets.Label(countRect, countText);
    GUI.color = Color.white;
    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;

    if (toggled || ClickedIdFree(rect)) {
      Toggle(window.CollapsedFeatures, featureKey);
    }
  }

  private static void DrawScenarioRow(Rect rect, RunnerWindow window, FeaturePlan plan, ScenarioPlan scenario, int scenarioIndex) {
    string sourcePath = plan.SourcePath ?? string.Empty;
    bool hasResult = window.TryGetResult(sourcePath, scenarioIndex, out ScenarioResult result);
    bool isSelected = window.Selected == (sourcePath, scenarioIndex);

    if (isSelected) {
      Widgets.DrawHighlightSelected(rect);
    } else {
      Widgets.DrawHighlightIfMouseover(rect);
    }

    Color dotColor = hasResult ? RunnerStatusColors.ForOutcome(result.Outcome) : RunnerStatusColors.Pending;
    if (ReferenceEquals(window.ActiveSession?.CurrentScenario, scenario)) {
      dotColor = window.ActiveSession?.IsPausedForBreak == true ? RunnerStatusColors.Failed : RunnerStatusColors.Keyword;
    }

    Rect checkRect = new Rect(rect.x + ScenarioIndent, rect.y + ((rect.height - ScenarioCheckboxSize) / 2f), ScenarioCheckboxSize, ScenarioCheckboxSize);
    bool selected = window.IsScenarioSelected(sourcePath, scenarioIndex);
    Widgets.CheckboxDraw(checkRect.x, checkRect.y, selected, disabled: false, ScenarioCheckboxSize);
    MouseoverSounds.DoRegion(checkRect);
    if (ClickedIdFree(checkRect)) {
      window.SetScenarioSelected(sourcePath, scenarioIndex, !selected);
      window.PublishSnapshot();
      PlayCheckboxSound(!selected);
    }

    float dotX = checkRect.xMax + 10f;
    RunnerStatusColors.DrawDot(new Vector2(dotX, rect.y + (rect.height / 2f)), dotColor, 6f);

    float labelX = dotX + 10f;
    Rect labelRect = new Rect(labelX, rect.y, rect.width - (labelX - rect.x) - 70f, rect.height);
    Text.Anchor = TextAnchor.MiddleLeft;
    Widgets.Label(labelRect, scenario.Name.Truncate(labelRect.width, ScenarioTruncation));

    if (hasResult) {
      Text.Font = GameFont.Tiny;
      GUI.color = RunnerStatusColors.Muted;
      Rect durationRect = new Rect(rect.xMax - 60f, rect.y, 56f, rect.height);
      Text.Anchor = TextAnchor.MiddleRight;
      Widgets.Label(durationRect, $"{result.DurationMs / 1000d:0.0}s");
      GUI.color = Color.white;
      Text.Font = GameFont.Small;
    }

    Text.Anchor = TextAnchor.UpperLeft;

    MouseoverSounds.DoRegion(rect);
    if (ClickedIdFree(rect)) {
      window.Selected = (sourcePath, scenarioIndex);
      window.FollowRun = false;
      window.DetailScroll = Vector2.zero;
    }
  }

  // Widgets' buttons and checkboxes allocate an IMGUI control id each; culling makes the row
  // count move with the scroll, which would shift the scrollbar's ids mid-drag. No ids here.
  private static bool ClickedIdFree(Rect rect) {
    if (Event.current.type != EventType.MouseDown || Event.current.button != 0 || !Mouse.IsOver(rect)) {
      return false;
    }

    Event.current.Use();
    return true;
  }

  private static MultiCheckboxState CheckboxMultiIdFree(Rect rect, MultiCheckboxState state) {
    Texture2D tex = state switch {
      MultiCheckboxState.On => Widgets.CheckboxOnTex,
      MultiCheckboxState.Off => Widgets.CheckboxOffTex,
      _ => Widgets.CheckboxPartialTex,
    };

    MouseoverSounds.DoRegion(rect);
    GUI.color = Mouse.IsOver(rect) ? GenUI.MouseoverColor : Color.white;
    GUI.DrawTexture(rect, tex);
    GUI.color = Color.white;

    if (!ClickedIdFree(rect)) {
      return state;
    }

    // Off goes to On; On or Partial go to Off, matching Widgets.CheckboxMulti.
    MultiCheckboxState next = state == MultiCheckboxState.Off ? MultiCheckboxState.On : MultiCheckboxState.Off;
    PlayCheckboxSound(next == MultiCheckboxState.On);
    return next;
  }

  private static bool DrawArrow(Rect rect, float x, bool collapsed) {
    Rect arrowRect = new Rect(rect.x + x, rect.y + ((rect.height - ArrowSize) / 2f), ArrowSize, ArrowSize);
    GUI.color = Mouse.IsOver(arrowRect) ? GenUI.MouseoverColor : Color.white;
    GUI.DrawTexture(arrowRect, collapsed ? TexButton.Reveal : TexButton.Collapse);
    GUI.color = Color.white;
    return ClickedIdFree(arrowRect);
  }

  private static void Toggle(HashSet<string> collapsed, string key) {
    if (!collapsed.Remove(key)) {
      collapsed.Add(key);
    }
  }

  private static void PlayCheckboxSound(bool on) {
    (on ? SoundDefOf.Checkbox_TurnedOn : SoundDefOf.Checkbox_TurnedOff).PlayOneShotOnCamera();
  }

  private static void SetSelected(
      RunnerWindow window, List<(string SourcePath, int Index)> keys, bool selected) {
    foreach ((string sourcePath, int index) in keys) {
      window.SetScenarioSelected(sourcePath, index, selected);
    }

    window.PublishSnapshot();
  }

  private static MultiCheckboxState ComputeCheckState(
      RunnerWindow window, List<(string SourcePath, int Index)> keys) {
    bool anySelected = false;
    bool anyUnselected = false;
    foreach ((string sourcePath, int index) in keys) {
      if (window.IsScenarioSelected(sourcePath, index)) {
        anySelected = true;
      } else {
        anyUnselected = true;
      }
    }

    if (anySelected && anyUnselected) {
      return MultiCheckboxState.Partial;
    }

    return anySelected ? MultiCheckboxState.On : MultiCheckboxState.Off;
  }

  private static Color RollupColor(int failed, int ran) {
    if (failed > 0) {
      return RunnerStatusColors.Failed;
    }

    return ran > 0 ? RunnerStatusColors.Passed : RunnerStatusColors.Pending;
  }

  private static string RollupCount(int failed, int ran, int total) {
    if (failed > 0) {
      return $"{failed}/{total} failed";
    }

    return ran > 0 ? $"{ran}/{total}" : "not run";
  }

  // One flat draw entry; Kind picks which of the three shapes is populated.
  private sealed class Row {
    public RowKind Kind { get; set; }

    public float Height { get; set; }

    public string ModName { get; set; } = string.Empty;

    public int ModScenarioCount { get; set; }

    public List<(string SourcePath, int Index)>? SelectionKeys { get; set; }

    public FeaturePlan? Plan { get; set; }

    public ScenarioPlan? Scenario { get; set; }

    public int Index { get; set; }
  }
}
