using Pickle.Core.Run;
using UnityEngine;
using Verse;

namespace Pickle.UI;

/// <summary>
/// Card shown when a step fails with break-on-failure armed. forcePause stops ticks
/// without absorbing input, so the map stays inspectable.
/// </summary>
public class BreakCard : Window {
  private const float CardWidth = 560f;
  private const float TitleHeight = 26f;
  private const float BreadcrumbHeight = 22f;
  private const float StepRowHeight = 24f;
  private const float MessageHeight = 48f;
  private const float HintHeight = 20f;
  private const float ButtonRowHeight = 32f;
  private const float Padding = 16f;
  private const float Gap = 10f;

  private readonly string featureName;
  private readonly string scenarioName;
  private readonly StepResult failingStep;

  public BreakCard(string featureName, string scenarioName, StepResult failingStep) {
    this.featureName = featureName;
    this.scenarioName = scenarioName;
    this.failingStep = failingStep;

    forcePause = true;
    doCloseX = false;
    closeOnClickedOutside = false;
    closeOnAccept = false;
    closeOnCancel = false;
    draggable = false;
    absorbInputAroundWindow = false;
  }

  public BreakCardDecision? Decision { get; private set; }

  /// <inheritdoc/>
  public override Vector2 InitialSize => new Vector2(
      CardWidth,
      Padding + TitleHeight + Gap + BreadcrumbHeight + Gap + StepRowHeight + Gap + MessageHeight + Gap + HintHeight + Gap + ButtonRowHeight + Padding);

  /// <inheritdoc/>
  public override void DoWindowContents(Rect inRect) {
    float y = inRect.y;

    Text.Font = GameFont.Medium;
    GUI.color = RunnerStatusColors.FailedText;
    Widgets.Label(new Rect(inRect.x, y, inRect.width, TitleHeight), "✗ Step failed — run paused");
    GUI.color = Color.white;
    Text.Font = GameFont.Small;
    y += TitleHeight + Gap;

    Widgets.Label(new Rect(inRect.x, y, inRect.width, BreadcrumbHeight), $"{scenarioName} · {featureName}");
    y += BreadcrumbHeight + Gap;

    RunnerStatusColors.DrawDot(new Vector2(inRect.x + 6f, y + (StepRowHeight / 2f)), RunnerStatusColors.Failed, 6f);
    Rect keywordRect = new Rect(inRect.x + 16f, y, 70f, StepRowHeight);
    GUI.color = RunnerStatusColors.Keyword;
    Widgets.Label(keywordRect, failingStep.Keyword);
    GUI.color = RunnerStatusColors.FailedText;
    Widgets.Label(new Rect(keywordRect.xMax + 4f, y, inRect.width - keywordRect.xMax - 4f, StepRowHeight), failingStep.Text);
    GUI.color = Color.white;
    y += StepRowHeight + Gap;

    GUI.color = RunnerStatusColors.FailedText;
    Widgets.Label(new Rect(inRect.x, y, inRect.width, MessageHeight), failingStep.FailureMessage ?? string.Empty);
    GUI.color = Color.white;
    y += MessageHeight + Gap;

    Text.Font = GameFont.Tiny;
    GUI.color = RunnerStatusColors.Muted;
    Widgets.Label(new Rect(inRect.x, y, inRect.width, HintHeight), "game is paused with the failing state live — inspect anything, then choose:");
    GUI.color = Color.white;
    Text.Font = GameFont.Small;
    y += HintHeight + Gap;

    float buttonWidth = (inRect.width - (Gap * 2f)) / 3f;
    Rect continueRect = new Rect(inRect.x, y, buttonWidth, ButtonRowHeight);
    Rect abortRect = new Rect(continueRect.xMax + Gap, y, buttonWidth, ButtonRowHeight);
    Rect openRect = new Rect(abortRect.xMax + Gap, y, buttonWidth, ButtonRowHeight);

    if (Widgets.ButtonText(continueRect, "Continue run")) {
      Decision = BreakCardDecision.Continue;
    }

    if (Widgets.ButtonText(abortRect, "Abort run")) {
      Decision = BreakCardDecision.Abort;
    }

    if (Widgets.ButtonText(openRect, "Open in results")) {
      Decision = BreakCardDecision.OpenInResults;
    }
  }
}
