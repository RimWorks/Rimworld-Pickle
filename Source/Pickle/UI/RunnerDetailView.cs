using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Pickle.Core.Discovery;
using Pickle.Core.Model;
using Pickle.Core.Run;
using UnityEngine;
using Verse;

namespace Pickle.UI;

/// <summary>
/// Right-pane scenario detail: steps, timings, and failure evidence. Steps come from
/// the ScenarioResult when one exists, otherwise from the parsed plan as pending.
/// </summary>
public static class RunnerDetailView {
  private const float HeaderHeight = 28f;
  private const float PathLineHeight = 20f;
  private const float StepRowHeight = 22f;
  private const float FailboxSpacing = 10f;
  private const float FailMessageHeight = 20f;
  private const float AttachmentsLineHeight = 18f;
  private const float LogLineHeight = 16f;
  private const int MaxLogLines = 6;

  private static readonly TimeSpan RegexTimeout = TimeSpan.FromSeconds(2);

  private static readonly Regex FixtureStepPattern = new Regex("the save \"([^\"]+)\" is loaded", RegexOptions.None, RegexTimeout);

  public static void Draw(Rect outRect, RunnerWindow window) {
    if (!window.TryGetSelectedScenario(out DiscoveredSuite suite, out FeaturePlan plan, out ScenarioPlan scenario, out int scenarioIndex)) {
      DrawEmptyState(outRect);
      return;
    }

    window.TryGetResult(plan.SourcePath ?? string.Empty, scenarioIndex, out ScenarioResult result);

    float contentHeight = MeasureHeight(scenario, result);
    Rect viewRect = new Rect(0f, 0f, outRect.width - 16f, contentHeight);

    Vector2 scroll = window.DetailScroll;
    Widgets.BeginScrollView(outRect, ref scroll, viewRect);
    window.DetailScroll = scroll;

    // Every row gets a fixed single-line height. Word wrap would push overflow into the
    // row below instead of clipping.
    Text.WordWrap = false;

    float y = 0f;

    Text.Font = GameFont.Medium;
    Widgets.Label(new Rect(0f, y, viewRect.width, HeaderHeight), scenario.Name);
    Text.Font = GameFont.Small;
    y += HeaderHeight;

    string fileName = plan.SourcePath == null ? plan.Name : System.IO.Path.GetFileName(plan.SourcePath);
    string breadcrumb = $"{suite.ModName} / {fileName}:{scenario.Line}";
    string? fixtureName = FindFixtureName(scenario);
    if (fixtureName != null) {
      breadcrumb += $" · fixture: {fixtureName}";
    }

    GUI.color = RunnerStatusColors.Muted;
    Text.Font = GameFont.Tiny;
    Widgets.Label(new Rect(0f, y, viewRect.width, PathLineHeight), breadcrumb);
    Text.Font = GameFont.Small;
    GUI.color = Color.white;
    y += PathLineHeight + FailboxSpacing;

    if (result != null) {
      foreach (StepResult step in result.Steps) {
        string durationText = step.Status switch {
          StepStatus.Skipped => "skipped",
          StepStatus.Failed when step.FailureMessage?.Contains("timed out") == true => "timed out",
          _ => $"{step.DurationMs:0}ms",
        };
        DrawStepRow(viewRect.width, y, step.Keyword, step.Text, RunnerStatusColors.ForStep(step.Status), durationText, step.Status == StepStatus.Failed);
        y += StepRowHeight;
      }
    } else {
      foreach (StepPlan step in scenario.Steps) {
        DrawStepRow(viewRect.width, y, step.Keyword, step.Text, RunnerStatusColors.Pending, null, false);
        y += StepRowHeight;
      }
    }

    if (result is { Outcome: ScenarioOutcome.Failed }) {
      y += FailboxSpacing;
      DrawFailureBox(new Rect(0f, y, viewRect.width, FailureBoxHeight(result)), result);
    }

    Text.WordWrap = true;
    Widgets.EndScrollView();
  }

  private static float MeasureHeight(ScenarioPlan scenario, ScenarioResult? result) {
    float height = HeaderHeight + PathLineHeight + FailboxSpacing;
    int stepCount = result?.Steps.Count ?? scenario.Steps.Count;
    height += stepCount * StepRowHeight;

    if (result is { Outcome: ScenarioOutcome.Failed }) {
      height += FailboxSpacing + FailureBoxHeight(result);
    }

    return height;
  }

  private static float FailureBoxHeight(ScenarioResult result) {
    int logLines = System.Math.Min(result.LogTail.Count, MaxLogLines);
    return FailMessageHeight + AttachmentsLineHeight + (logLines * LogLineHeight) + (FailboxSpacing * 2f);
  }

  private static string? FindFixtureName(ScenarioPlan scenario) {
    foreach (StepPlan step in scenario.Steps) {
      Match match = FixtureStepPattern.Match(step.Text);
      if (match.Success) {
        return match.Groups[1].Value;
      }
    }

    return null;
  }

  private static void DrawStepRow(float width, float y, string keyword, string text, Color dotColor, string? durationText, bool failed) {
    Rect rowRect = new Rect(0f, y, width, StepRowHeight);
    RunnerStatusColors.DrawDot(new Vector2(6f, y + (StepRowHeight / 2f)), dotColor, 6f);

    Rect keywordRect = new Rect(16f, y, 70f, StepRowHeight);
    GUI.color = RunnerStatusColors.Keyword;
    Text.Anchor = TextAnchor.MiddleLeft;
    Widgets.Label(keywordRect, keyword);
    GUI.color = Color.white;

    Rect textRect = new Rect(keywordRect.xMax + 4f, y, width - keywordRect.xMax - 70f, StepRowHeight);
    if (failed) {
      GUI.color = RunnerStatusColors.FailedText;
    }

    Widgets.Label(textRect, text);
    GUI.color = Color.white;

    if (durationText != null) {
      Text.Font = GameFont.Tiny;
      GUI.color = RunnerStatusColors.Muted;
      Rect durationRect = new Rect(rowRect.xMax - 60f, y, 56f, StepRowHeight);
      Text.Anchor = TextAnchor.MiddleRight;
      Widgets.Label(durationRect, durationText);
      GUI.color = Color.white;
      Text.Font = GameFont.Small;
    }

    Text.Anchor = TextAnchor.UpperLeft;
  }

  private static void DrawFailureBox(Rect rect, ScenarioResult result) {
    Widgets.DrawBoxSolidWithOutline(rect, new Color(0.780f, 0.392f, 0.353f, 0.08f), RunnerStatusColors.Failed);

    float y = rect.y + FailboxSpacing;
    GUI.color = RunnerStatusColors.FailedText;
    Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 20f, FailMessageHeight), result.FailureMessage ?? string.Empty);
    GUI.color = Color.white;
    y += FailMessageHeight;

    List<string> attachmentNames = [.. result.Attachments.Select(a => a.Name),
.. result.StateDumps.Select(d => d.Source)];
    if (result.LogTail.Count > 0) {
      attachmentNames.Add("log-tail.txt");
    }

    if (attachmentNames.Count > 0) {
      Text.Font = GameFont.Tiny;
      GUI.color = RunnerStatusColors.Muted;
      Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 20f, AttachmentsLineHeight), string.Join(" · ", attachmentNames));
      GUI.color = Color.white;
      Text.Font = GameFont.Small;
    }

    y += AttachmentsLineHeight;

    Text.Font = GameFont.Tiny;
    GUI.color = RunnerStatusColors.Muted;
    foreach (string line in result.LogTail.Take(MaxLogLines)) {
      Widgets.Label(new Rect(rect.x + 10f, y, rect.width - 20f, LogLineHeight), line);
      y += LogLineHeight;
    }

    GUI.color = Color.white;
    Text.Font = GameFont.Small;
  }

  private static void DrawEmptyState(Rect outRect) {
    GUI.color = RunnerStatusColors.Muted;
    Text.Anchor = TextAnchor.MiddleCenter;
    Widgets.Label(outRect, "Select a scenario to see its steps.");
    Text.Anchor = TextAnchor.UpperLeft;
    GUI.color = Color.white;
  }
}
