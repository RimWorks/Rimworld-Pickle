using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

public static class RunnerDetailView {
  private static readonly Regex FixtureStepPattern = new Regex("the save \"([^\"]+)\" is loaded", RegexOptions.None, TimeSpan.FromSeconds(2));

  public static void Draw(Rect outRect, RunnerWindow window) {
    if (!window.TryGetSelectedScenario(out DiscoveredSuite suite, out FeaturePlan plan, out ScenarioPlan scenario, out int index)) {
      Text.Anchor = TextAnchor.MiddleCenter;
      Widgets.Label(outRect, "Pickle_SelectScenario".Translate());
      Text.Anchor = TextAnchor.UpperLeft;
      return;
    }

    window.TryGetResult(plan.SourcePath ?? string.Empty, index, out ScenarioResult result);
    bool live = ReferenceEquals(window.ActiveSession?.CurrentScenario, scenario);
    IReadOnlyList<StepResult> steps = live ? window.ActiveSession!.CurrentStepResults : result?.Steps ?? [];
    if (live) {
      result = null!;
    }

    Text.WordWrap = true;
    float width = outRect.width - 20f;
    float height = Content(width, window, suite, plan, scenario, steps, result, live, false);
    Vector2 scroll = window.DetailScroll;
    Widgets.BeginScrollView(outRect, ref scroll, new Rect(0f, 0f, width, height));
    window.DetailScroll = scroll;
    _ = Content(width, window, suite, plan, scenario, steps, result, live, true);
    Widgets.EndScrollView();
    Text.Font = GameFont.Small;
    GUI.color = Color.white;
  }

  private static float Content(float width, RunnerWindow window, DiscoveredSuite suite, FeaturePlan plan, ScenarioPlan scenario,
      IReadOnlyList<StepResult> steps, ScenarioResult? result, bool live, bool draw) {
    float y = 0f;
    Label(scenario.Name, width, ref y, GameFont.Medium, Color.white, draw);
    if (!window.IsScenarioVisible(suite, plan, scenario)) {
      Label("This scenario is hidden by the current filters.", width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);
    }
    string location = $"{suite.ModName} / {Path.GetFileName(plan.SourcePath ?? plan.Name)}:{scenario.Line}";
    string? fixture = scenario.Steps.Select(step => FixtureStepPattern.Match(step.Text)).FirstOrDefault(match => match.Success)?.Groups[1].Value;
    Label(fixture == null ? location : location + $" · fixture: {fixture}", width, ref y, GameFont.Tiny, Color.white, draw);
    Label(live ? window.ActiveSession!.CurrentStepDisplay : result == null ? "Pending" : $"{result.Outcome} · {result.DurationMs:0}ms", width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);

    float tagX = 0f;
    foreach (string tag in scenario.Tags) {
      Text.Font = GameFont.Tiny;
      float tagWidth = Math.Min(width, Text.CalcSize(tag).x + 16f);
      if (tagX + tagWidth > width) {
        tagX = 0f;
        y += 28f;
      }

      if (draw && Widgets.ButtonText(new Rect(tagX, y, tagWidth, 24f), tag)) {
        window.SetFilter(tag: tag, additive: Event.current.shift);
      }

      tagX += tagWidth + 6f;
    }

    y += tagX > 0f ? 36f : 8f;
    for (int i = 0; i < (result == null ? Math.Max(steps.Count, scenario.Steps.Count) : steps.Count); i++) {
      StepResult? step = i < steps.Count ? steps[i] : null;
      StepPlan? pending = i < scenario.Steps.Count ? scenario.Steps[i] : null;
      string text = $"{step?.Keyword ?? pending?.Keyword} {step?.Text ?? pending?.Text}";
      string status = step == null ? "Pending" : $"{step.Status} · {step.DurationMs:0}ms";
      Color color = step == null ? Color.white : RunnerStatusColors.ForStep(step.Status);
      Label(text, width, ref y, GameFont.Small, color, draw);
      Label(status, width, ref y, GameFont.Tiny, color, draw);
      if (!string.IsNullOrEmpty(step?.FailureMessage)) {
        Label(step!.FailureMessage!, width, ref y, GameFont.Small, RunnerStatusColors.FailedText, draw);
      }

      y += 6f;
    }

    if (result == null) {
      return y;
    }

    if (!string.IsNullOrEmpty(result.FailureMessage)) {
      Label(result.FailureMessage!, width, ref y, GameFont.Small, RunnerStatusColors.FailedText, draw);
    }

    List<(string Name, string Content)> attachments = [.. EvidenceAttachments.Expand(result.Attachments)];
    List<string> frames = [.. attachments.Where(attachment => attachment.Name == "film-frames").Select(attachment => attachment.Content)];
    if (frames.Count > 0) {
      if (draw && Widgets.ButtonText(new Rect(0f, y, width, 28f), $"Filmstrip ({frames.Count} frames)")) {
        Find.WindowStack.Add(new EvidenceWindow("Pickle_Filmstrip".Translate(), frames));
      }

      y += 36f;
    }

    foreach ((string name, string content) in attachments.Where(attachment => attachment.Name != "film-frames")) {
      Label(name, width, ref y, GameFont.Small, Color.white, draw);
      bool image = content.StartsWith("data:image/", StringComparison.Ordinal)
          || content.EndsWith(".png", StringComparison.OrdinalIgnoreCase) || content.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase)
          || content.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase);
      if (image || name == "film-video") {
        if (draw && Widgets.ButtonText(new Rect(0f, y, width, 28f), image ? "Open image" : "Open film")) {
          Find.WindowStack.Add(new EvidenceWindow(name, [content], name == "film-video"));
        }

        y += 36f;
      } else {
        Label(content, width, ref y, GameFont.Tiny, Color.white, draw);
      }
    }

    foreach ((string source, string content) in result.StateDumps) {
      Label(source, width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);
      Label(content, width, ref y, GameFont.Tiny, Color.white, draw);
    }

    // A flake that only shows a green dot teaches nobody anything, so the attempts it
    // burnt and what each one said are part of the result.
    if (result.FailedAttempts.Count > 0) {
      Label(
          "Pickle_EarlierAttempts".Translate(result.Attempts),
          width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);

      foreach ((int attempt, string? message) in result.FailedAttempts) {
        Label($"{attempt}: {message ?? "Scenario failed"}", width, ref y, GameFont.Tiny, Color.white, draw);
      }
    }

    if (result.LogTail.Count > 0) {
      Label("Pickle_LogTail".Translate(), width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);
      Label(string.Join("\n", result.LogTail), width, ref y, GameFont.Tiny, Color.white, draw);
    }

    return y;
  }

  private static void Label(string text, float width, ref float y, GameFont font, Color color, bool draw) {
    Text.Font = font;
    float height = Text.CalcHeight(text, width) + 4f;
    if (draw) {
      GUI.color = color;
      Widgets.Label(new Rect(0f, y, width, height), text);
      GUI.color = Color.white;
    }

    y += height;
  }
}
