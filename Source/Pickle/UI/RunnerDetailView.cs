using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>The right-hand panel of the runner window: one scenario's steps, tags, and, once it has run, its outcome and evidence.</summary>
public static class RunnerDetailView {
  private const float KeywordColumn = 52f;
  private const float DurationColumn = 56f;
  private const float CardInset = 12f;
  private const string ArgumentOpen = "<color=#8FD3E8>";
  private const string ArgumentClose = "</color>";
  private static readonly Color CardBackground = new Color(0.157f, 0.184f, 0.224f);
  private static readonly Color CardHeaderBackground = new Color(0.133f, 0.161f, 0.200f);
  private static readonly Color CardLine = new Color(0.278f, 0.322f, 0.357f);
  private static readonly Color StepText = new Color(0.902f, 0.929f, 0.929f);
  private static readonly Color Faded = new Color(0.753f, 0.737f, 0.682f, 0.55f);
  private static readonly Regex FixtureStepPattern = new Regex("the save \"([^\"]+)\" is loaded", RegexOptions.None, TimeSpan.FromSeconds(2));

  /// <summary>Draws the panel for whichever scenario is selected in the tree, or a prompt when nothing is selected.</summary>
  /// <param name="outRect">The area to draw into.</param>
  /// <param name="window">The runner window, read for the current selection, filters, and live session.</param>
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
    Detail detail = new Detail(window, suite, plan, scenario, steps, result, live);
    float height = Content(width, detail, false);
    Vector2 scroll = window.DetailScroll;
    Widgets.BeginScrollView(outRect, ref scroll, new Rect(0f, 0f, width, height));
    window.DetailScroll = scroll;
    _ = Content(width, detail, true);
    Widgets.EndScrollView();
    Text.Font = GameFont.Small;
    GUI.color = Color.white;
  }

  private static float Content(float width, in Detail detail, bool draw) {
    float inner = width - (CardInset * 2f);
    float band = 0f;
    Header(inner, detail, ref band, false);
    Tags(inner, detail, ref band, false);
    float steps = 0f;
    StepLines(inner, detail, ref steps, false);
    float bandHeight = band + (CardInset * 2f);
    float cardHeight = bandHeight + steps + CardInset;

    if (draw) {
      Widgets.DrawBoxSolid(new Rect(0f, 0f, width, cardHeight), CardBackground);
      Widgets.DrawBoxSolid(new Rect(0f, 0f, width, bandHeight), CardHeaderBackground);
      Widgets.DrawLineHorizontal(0f, bandHeight, width, CardLine);
      GUI.color = CardLine;
      Widgets.DrawBox(new Rect(0f, 0f, width, cardHeight));
      GUI.color = Color.white;

      GUI.BeginGroup(new Rect(CardInset, CardInset, inner, band));
      float head = 0f;
      Header(inner, detail, ref head, true);
      Tags(inner, detail, ref head, true);
      GUI.EndGroup();

      GUI.BeginGroup(new Rect(CardInset, bandHeight, inner, steps));
      float list = 0f;
      StepLines(inner, detail, ref list, true);
      GUI.EndGroup();
    }

    float y = cardHeight + 12f;
    if (detail.Result != null) {
      Outcome(width, detail.Result, ref y, draw);
    }

    return y;
  }

  private static void Header(float width, in Detail detail, ref float y, bool draw) {
    ScenarioPlan scenario = detail.Scenario;
    Label(scenario.Name, width, ref y, GameFont.Medium, Color.white, draw);
    if (!detail.Window.IsScenarioVisible(detail.Suite, detail.Plan, scenario)) {
      Label("This scenario is hidden by the current filters.", width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);
    }

    string location = $"{detail.Suite.ModName} / {Path.GetFileName(detail.Plan.SourcePath ?? detail.Plan.Name)}:{scenario.Line}";
    Label(location, width, ref y, GameFont.Tiny, RunnerStatusColors.Muted, draw);
    string? fixture = scenario.Steps.Select(step => FixtureStepPattern.Match(step.Text)).FirstOrDefault(match => match.Success)?.Groups[1].Value;
    if (fixture != null) {
      Label($"Fixture: {fixture}", width, ref y, GameFont.Tiny, RunnerStatusColors.Muted, draw);
    }

    Label(OutcomeText(detail), width, ref y, GameFont.Tiny, RunnerStatusColors.Keyword, draw);
  }

  private static string OutcomeText(in Detail detail) {
    if (detail.Live) {
      return detail.Window.ActiveSession!.CurrentStepDisplay;
    }

    return detail.Result == null ? "Pending" : $"{detail.Result.Outcome} in {detail.Result.DurationMs:0}ms";
  }

  private static void Tags(float width, in Detail detail, ref float y, bool draw) {
    float tagX = 0f;
    foreach (string tag in detail.Scenario.Tags) {
      Text.Font = GameFont.Tiny;
      float tagWidth = Math.Min(width, Text.CalcSize(tag).x + 16f);
      if (tagX + tagWidth > width) {
        tagX = 0f;
        y += 28f;
      }

      if (draw && Widgets.ButtonText(new Rect(tagX, y, tagWidth, 24f), tag)) {
        detail.Window.SetFilter(tag: tag, additive: Event.current.shift);
      }

      tagX += tagWidth + 6f;
    }

    y += tagX > 0f ? 36f : 8f;
  }

  private static void StepLines(float width, in Detail detail, ref float y, bool draw) {
    IReadOnlyList<StepResult> steps = detail.Steps;
    int count = detail.Result == null ? Math.Max(steps.Count, detail.Scenario.Steps.Count) : steps.Count;
    for (int i = 0; i < count; i++) {
      StepResult? step = i < steps.Count ? steps[i] : null;
      StepPlan? pending = i < detail.Scenario.Steps.Count ? detail.Scenario.Steps[i] : null;
      string keyword = (step?.Keyword ?? pending?.Keyword ?? string.Empty).Trim();
      bool tucked = keyword is "And" or "But";
      string marked = StepArgumentMarkup.Apply(step?.Text ?? pending?.Text ?? string.Empty, step?.ArgumentSpans, ArgumentOpen, ArgumentClose);
      StepRow(width, new StepRowInfo(keyword, marked, step, tucked, i == 0), ref y, draw);
      if (step is { FailureMessage.Length: > 0 }) {
        Text.Font = GameFont.Small;
        float height = Text.CalcHeight(step.FailureMessage, width - KeywordColumn - 12f) + 4f;
        if (draw) {
          GUI.color = RunnerStatusColors.FailedText;
          Widgets.Label(new Rect(KeywordColumn + 12f, y, width - KeywordColumn - 12f, height), step.FailureMessage);
          GUI.color = Color.white;
        }

        y += height + 4f;
      }
    }
  }

  private static void StepRow(float width, in StepRowInfo row, ref float y, bool draw) {
    float textWidth = width - KeywordColumn - 17f - DurationColumn;
    Text.Font = GameFont.Small;
    float rowHeight = Math.Max(Text.CalcHeight(row.Text, textWidth), 22f) + (row.Tucked ? 4f : 8f);
    if (draw) {
      DrawStepRow(width, row, y, rowHeight);
    }

    y += rowHeight;
  }

  private static void DrawStepRow(float width, in StepRowInfo row, float y, float rowHeight) {
    if (!row.First && !row.Tucked) {
      Widgets.DrawLineHorizontal(0f, y, width, CardLine);
    }

    StepResult? step = row.Step;
    Color dot = step == null ? RunnerStatusColors.Pending : RunnerStatusColors.ForStep(step.Status);
    bool dim = step == null || step.Status == StepStatus.Skipped;
    float baseline = y + (row.Tucked ? 2f : 4f);

    Text.Font = GameFont.Tiny;
    Text.Anchor = TextAnchor.UpperRight;
    GUI.color = row.Tucked ? Faded : RunnerStatusColors.Muted;
    Widgets.Label(new Rect(0f, baseline + 1f, KeywordColumn, 20f), row.Keyword);

    Widgets.Label(new Rect(width - DurationColumn, baseline + 1f, DurationColumn, 20f),
        step == null ? string.Empty : $"{step.DurationMs:0}ms");
    Text.Anchor = TextAnchor.UpperLeft;

    RunnerStatusColors.DrawDot(new Vector2(KeywordColumn + 7f, baseline + 9f), dot, 7f);

    float textX = KeywordColumn + 17f;
    Text.Font = GameFont.Small;
    GUI.color = dim ? Faded : StepText;
    Widgets.Label(new Rect(textX, baseline, width - textX - DurationColumn, rowHeight), row.Text);
    GUI.color = Color.white;
  }

  private static void Outcome(float width, ScenarioResult result, ref float y, bool draw) {
    if (result.FailureMessage is { Length: > 0 }) {
      Label(result.FailureMessage, width, ref y, GameFont.Small, RunnerStatusColors.FailedText, draw);
    }

    Attachments(width, result, ref y, draw);

    foreach ((string source, string content) in result.StateDumps) {
      Label(source, width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);
      Label(content, width, ref y, GameFont.Tiny, Color.white, draw);
    }

    Attempts(width, result, ref y, draw);

    if (result.LogTail.Count > 0) {
      Label("Pickle_LogTail".Translate(), width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);
      Label(string.Join("\n", result.LogTail), width, ref y, GameFont.Tiny, Color.white, draw);
    }
  }

  private static void Attachments(float width, ScenarioResult result, ref float y, bool draw) {
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
  }

  // A flake that only shows a green dot teaches nobody anything, so the attempts it
  // burnt and what each one said are part of the result.
  private static void Attempts(float width, ScenarioResult result, ref float y, bool draw) {
    if (result.FailedAttempts.Count == 0) {
      return;
    }

    Label(
        "Pickle_EarlierAttempts".Translate(result.Attempts),
        width, ref y, GameFont.Small, RunnerStatusColors.Keyword, draw);

    foreach ((int attempt, string? message) in result.FailedAttempts) {
      Label($"{attempt}: {message ?? "Scenario failed"}", width, ref y, GameFont.Tiny, Color.white, draw);
    }
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

  // Nine arguments threaded through every section became one bundle. Sections take it by
  // reference so the layout pass and the draw pass stay a single code path.
  private readonly struct Detail {
    public Detail(RunnerWindow window, DiscoveredSuite suite, FeaturePlan plan, ScenarioPlan scenario,
        IReadOnlyList<StepResult> steps, ScenarioResult? result, bool live) {
      Window = window;
      Suite = suite;
      Plan = plan;
      Scenario = scenario;
      Steps = steps;
      Result = result;
      Live = live;
    }

    public RunnerWindow Window { get; }

    public DiscoveredSuite Suite { get; }

    public FeaturePlan Plan { get; }

    public ScenarioPlan Scenario { get; }

    public IReadOnlyList<StepResult> Steps { get; }

    public ScenarioResult? Result { get; }

    public bool Live { get; }
  }

  private readonly struct StepRowInfo {
    public StepRowInfo(string keyword, string text, StepResult? step, bool tucked, bool first) {
      Keyword = keyword;
      Text = text;
      Step = step;
      Tucked = tucked;
      First = first;
    }

    public string Keyword { get; }

    public string Text { get; }

    public StepResult? Step { get; }

    public bool Tucked { get; }

    public bool First { get; }
  }
}
