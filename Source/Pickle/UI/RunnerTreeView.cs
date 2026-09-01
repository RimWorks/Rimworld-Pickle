using System;
using System.Collections.Generic;
using System.Linq;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Left-pane tree with tri-state checkboxes that derive from their scenario children.
/// Indices are numbered as RunAsync numbers them, so a row matches its stored result.
/// </summary>
public static class RunnerTreeView {
  private const float ModRowHeight = 26f;
  private const float FeatureRowHeight = 22f;
  private const float ScenarioRowHeight = 20f;
  private const float ModIndent = 6f;
  private const float FeatureIndent = 26f;
  private const float ScenarioIndent = 44f;
  private const float ModCheckboxSize = 24f;
  private const float FeatureCheckboxSize = 18f;
  private const float ScenarioCheckboxSize = 16f;

  public static void Draw(Rect outRect, RunnerWindow window) {
    Dictionary<FeaturePlan, int> startIndices = ComputeStartIndices(window);
    float contentHeight = MeasureHeight(window);
    Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, contentHeight);

    Vector2 scroll = window.TreeScroll;
    Widgets.BeginScrollView(outRect, ref scroll, viewRect);
    window.TreeScroll = scroll;

    // Rows are fixed-height single lines; word wrap would push overflow text
    // into the row below instead of clipping, corrupting the whole tree.
    Text.WordWrap = false;

    float y = 0f;
    foreach (IGrouping<string, (DiscoveredSuite Suite, FeaturePlan Plan)> group in window.ParsedFeatures.GroupBy(f => f.Suite.ModName)) {
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> features = [.. group.Where(f => FeatureHasVisibleScenario(window, f.Suite, f.Plan))];

      if (features.Count == 0) {
        continue;
      }

      // Uses the unfiltered group so the mod checkbox always covers every scenario. A search
      // that hides rows should not shrink what select-all means.
      List<(FeaturePlan Plan, int StartIndex)> modFeatures = [.. group.Select(f => (f.Plan, startIndices[f.Plan]))];
      int modScenarioCount = group.Sum(f => f.Plan.Scenarios.Count);
      DrawModRow(new Rect(0f, y, viewRect.width, ModRowHeight), window, group.Key, modScenarioCount, modFeatures);
      y += ModRowHeight;

      foreach ((DiscoveredSuite suite, FeaturePlan plan) in features) {
        int startIndex = startIndices[plan];
        DrawFeatureRow(new Rect(0f, y, viewRect.width, FeatureRowHeight), window, plan, startIndex);
        y += FeatureRowHeight;

        for (int i = 0; i < plan.Scenarios.Count; i++) {
          ScenarioPlan scenario = plan.Scenarios[i];
          int scenarioIndex = startIndex + i;
          if (!IsScenarioVisible(window, suite, plan, scenario)) {
            continue;
          }

          DrawScenarioRow(new Rect(0f, y, viewRect.width, ScenarioRowHeight), window, plan, scenario, scenarioIndex);
          y += ScenarioRowHeight;
        }
      }
    }

    Text.WordWrap = true;
    Widgets.EndScrollView();
  }

  private static float MeasureHeight(RunnerWindow window) {
    float height = 0f;
    foreach (IGrouping<string, (DiscoveredSuite Suite, FeaturePlan Plan)> group in window.ParsedFeatures.GroupBy(f => f.Suite.ModName)) {
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> features = [.. group.Where(f => FeatureHasVisibleScenario(window, f.Suite, f.Plan))];

      if (features.Count == 0) {
        continue;
      }

      height += ModRowHeight;
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in features) {
        height += FeatureRowHeight;
        height += plan.Scenarios.Count(s => IsScenarioVisible(window, suite, plan, s)) * ScenarioRowHeight;
      }
    }

    return height;
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
    if (window.ModFilterSelection != null && suite.ModName != window.ModFilterSelection) {
      return false;
    }

    if (window.ActiveTagFilters.Count > 0 && !window.ActiveTagFilters.All(t => scenario.Tags.Contains(t))) {
      return false;
    }

    if (string.IsNullOrEmpty(window.SearchText)) {
      return true;
    }

    return scenario.Name.IndexOf(window.SearchText, StringComparison.OrdinalIgnoreCase) >= 0
        || plan.Name.IndexOf(window.SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
  }

  private static void DrawModRow(Rect rect, RunnerWindow window, string modName, int scenarioCount, List<(FeaturePlan Plan, int StartIndex)> modFeatures) {
    Widgets.DrawHighlightIfMouseover(rect);

    MultiCheckboxState state = ComputeCheckState(window, modFeatures);
    Rect checkRect = new Rect(rect.x + ModIndent, rect.y + ((rect.height - ModCheckboxSize) / 2f), ModCheckboxSize, ModCheckboxSize);
    MultiCheckboxState newState = Widgets.CheckboxMulti(checkRect, state);
    if (newState != state) {
      bool selected = newState == MultiCheckboxState.On;
      foreach ((FeaturePlan plan, int startIndex) in modFeatures) {
        SetScenariosSelected(window, plan, startIndex, selected);
      }
    }

    Rect labelRect = new Rect(checkRect.xMax + 6f, rect.y, rect.width - checkRect.xMax - 90f, rect.height);
    Text.Anchor = TextAnchor.MiddleLeft;
    Widgets.Label(labelRect, RunnerStatusColors.Ellipsize(modName, labelRect.width));

    Text.Font = GameFont.Tiny;
    GUI.color = RunnerStatusColors.Muted;
    Rect countRect = new Rect(rect.xMax - 80f, rect.y, 76f, rect.height);
    Text.Anchor = TextAnchor.MiddleRight;
    Widgets.Label(countRect, $"{scenarioCount} scenarios");
    GUI.color = Color.white;
    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;
  }

  private static void DrawFeatureRow(Rect rect, RunnerWindow window, FeaturePlan plan, int startIndex) {
    Widgets.DrawHighlightIfMouseover(rect);

    MultiCheckboxState state = ComputeCheckState(window, plan, startIndex);
    Rect checkRect = new Rect(rect.x + FeatureIndent, rect.y + ((rect.height - FeatureCheckboxSize) / 2f), FeatureCheckboxSize, FeatureCheckboxSize);
    MultiCheckboxState newState = Widgets.CheckboxMulti(checkRect, state);
    if (newState != state) {
      SetScenariosSelected(window, plan, startIndex, newState == MultiCheckboxState.On);
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
    Widgets.Label(labelRect, RunnerStatusColors.Ellipsize(plan.Name, labelRect.width));

    Text.Font = GameFont.Tiny;
    GUI.color = failed > 0 ? RunnerStatusColors.FailedText : RunnerStatusColors.Muted;
    Rect countRect = new Rect(rect.xMax - 90f, rect.y, 86f, rect.height);
    Text.Anchor = TextAnchor.MiddleRight;
    Widgets.Label(countRect, countText);
    GUI.color = Color.white;
    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;
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

    Rect checkRect = new Rect(rect.x + ScenarioIndent, rect.y + ((rect.height - ScenarioCheckboxSize) / 2f), ScenarioCheckboxSize, ScenarioCheckboxSize);
    bool selected = window.IsScenarioSelected(sourcePath, scenarioIndex);
    bool newSelected = selected;
    Widgets.Checkbox(checkRect.position, ref newSelected, ScenarioCheckboxSize);
    if (newSelected != selected) {
      window.SetScenarioSelected(sourcePath, scenarioIndex, newSelected);
    }

    float dotX = checkRect.xMax + 10f;
    RunnerStatusColors.DrawDot(new Vector2(dotX, rect.y + (rect.height / 2f)), dotColor, 6f);

    float labelX = dotX + 10f;
    Rect labelRect = new Rect(labelX, rect.y, rect.width - (labelX - rect.x) - 70f, rect.height);
    Text.Anchor = TextAnchor.MiddleLeft;
    Widgets.Label(labelRect, RunnerStatusColors.Ellipsize(scenario.Name, labelRect.width));

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

    if (Widgets.ButtonInvisible(rect)) {
      window.Selected = (sourcePath, scenarioIndex);
    }
  }

  private static void SetScenariosSelected(RunnerWindow window, FeaturePlan plan, int startIndex, bool selected) {
    string sourcePath = plan.SourcePath ?? string.Empty;
    for (int i = 0; i < plan.Scenarios.Count; i++) {
      window.SetScenarioSelected(sourcePath, startIndex + i, selected);
    }
  }

  private static MultiCheckboxState ComputeCheckState(RunnerWindow window, FeaturePlan plan, int startIndex) {
    string sourcePath = plan.SourcePath ?? string.Empty;
    bool anySelected = false;
    bool anyUnselected = false;
    for (int i = 0; i < plan.Scenarios.Count; i++) {
      if (window.IsScenarioSelected(sourcePath, startIndex + i)) {
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

  private static MultiCheckboxState ComputeCheckState(RunnerWindow window, List<(FeaturePlan Plan, int StartIndex)> features) {
    bool anySelected = false;
    bool anyUnselected = false;
    foreach ((FeaturePlan plan, int startIndex) in features) {
      string sourcePath = plan.SourcePath ?? string.Empty;
      for (int i = 0; i < plan.Scenarios.Count; i++) {
        if (window.IsScenarioSelected(sourcePath, startIndex + i)) {
          anySelected = true;
        } else {
          anyUnselected = true;
        }
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
}
