using Pickle.Run;
using UnityEngine;
using Verse;

namespace Pickle.UI;

/// <summary>
/// Top-right pill shown while the full window is collapsed. Reads live state off the
/// owning RunSession rather than caching, so it never goes stale.
/// </summary>
public class RunPill : Window {
  private const float PillWidth = 360f;
  private const float Padding = 10f;
  private const float Gap = 8f;
  private const float TitleRowHeight = 20f;
  private const float StepRowHeight = 18f;
  private const float ButtonRowHeight = 28f;
  private const float ButtonWidth = 70f;

  private readonly RunnerWindow owner;

  public RunPill(RunnerWindow owner) {
    this.owner = owner;

    doCloseX = false;
    closeOnClickedOutside = false;
    closeOnAccept = false;
    closeOnCancel = false;
    draggable = false;
  }

  /// <inheritdoc/>
  public override Vector2 InitialSize => new Vector2(
      PillWidth,
      (Padding * 2f) + TitleRowHeight + Gap + StepRowHeight + Gap + ButtonRowHeight);

  // Window.PreOpen() always centers via InitialSize; overriding this is the only
  // way to land the pill top-right instead, matching the approved mock.

  /// <inheritdoc/>
  protected override void SetInitialSizeAndPosition() {
    Vector2 size = InitialSize;
    windowRect = new Rect(Verse.UI.screenWidth - size.x - 14f, 14f, size.x, size.y);
  }

  /// <inheritdoc/>
  public override void DoWindowContents(Rect inRect) {
    RunSession? session = owner.ActiveSession;
    bool paused = session?.IsPausedForBreak ?? false;

    float y = inRect.y;

    Color dotColor = paused ? RunnerStatusColors.Failed : RunnerStatusColors.Passed;
    RunnerStatusColors.DrawDot(new Vector2(inRect.x + 6f, y + (TitleRowHeight / 2f)), dotColor, 8f);

    string title = paused ? $"Paused — {session?.CurrentScenarioName}" : $"Running — {session?.CurrentScenarioName}";
    GUI.color = paused ? RunnerStatusColors.FailedText : Color.white;
    Widgets.Label(new Rect(inRect.x + 18f, y, inRect.width - 18f, TitleRowHeight), title);
    GUI.color = Color.white;
    y += TitleRowHeight + Gap;

    Text.Font = GameFont.Tiny;
    GUI.color = RunnerStatusColors.Muted;
    Widgets.Label(new Rect(inRect.x + 18f, y, inRect.width - 18f, StepRowHeight), session?.CurrentStepDisplay ?? string.Empty);
    GUI.color = Color.white;
    Text.Font = GameFont.Small;
    y += StepRowHeight + Gap;

    Rect expandRect = new Rect(inRect.x, y, ButtonWidth, ButtonRowHeight);
    Rect abortRect = new Rect(expandRect.xMax + Gap, y, ButtonWidth, ButtonRowHeight);

    if (Widgets.ButtonText(expandRect, "Expand")) {
      owner.ExpandFromPill();
    }

    if (Widgets.ButtonText(abortRect, "Abort")) {
      session?.RequestCancel();
    }

    Text.Font = GameFont.Tiny;
    Text.Anchor = TextAnchor.MiddleRight;

    string failedText = $"{session?.FailedCount ?? 0} ✗";
    string passedText = $"{session?.PassedCount ?? 0} ✓";
    Vector2 failedSize = Text.CalcSize(failedText);
    Vector2 passedSize = Text.CalcSize(passedText);

    Rect failedRect = new Rect(inRect.xMax - failedSize.x, y, failedSize.x, ButtonRowHeight);
    GUI.color = RunnerStatusColors.FailedText;
    Widgets.Label(failedRect, failedText);

    Rect sepRect = new Rect(failedRect.x - 14f, y, 14f, ButtonRowHeight);
    GUI.color = RunnerStatusColors.Muted;
    Widgets.Label(sepRect, "·");

    Rect passedRect = new Rect(sepRect.x - passedSize.x, y, passedSize.x, ButtonRowHeight);
    GUI.color = RunnerStatusColors.Passed;
    Widgets.Label(passedRect, passedText);

    GUI.color = Color.white;
    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;
  }
}
