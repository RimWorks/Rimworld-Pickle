using RimWorks.Pickle.Run;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Top-right pill shown while the full window is collapsed. Reads live state off the
/// owning RunSession rather than caching, so it never goes stale.
/// </summary>
public class RunPill : Window {
  private const float PillWidth = 360f;
  private const float Gap = 8f;
  private const float ButtonRowHeight = 28f;
  private const float ButtonWidth = 70f;

  // Leaves room for the status dot, which draws in the gutter to the left of both labels.
  private const float TextIndent = 18f;

  private const float ScreenInset = 14f;

  private readonly RunnerWindow owner;

  // Measured while drawing, applied on the next frame. CalcHeight reads the IMGUI font
  // style, which is only trustworthy inside OnGUI, and WindowUpdate is not.
  private float titleHeight;
  private float stepHeight;

  public RunPill(RunnerWindow owner) {
    this.owner = owner;

    doCloseX = false;
    closeOnClickedOutside = false;
    closeOnAccept = false;
    closeOnCancel = false;
    draggable = false;
  }

  /// <inheritdoc/>
  // Margin, not a padding of our own: Window.InnerWindowOnGUI contracts the rect it
  // hands DoWindowContents by exactly that, so a smaller guess clips the button row.
  public override Vector2 InitialSize => new Vector2(
      PillWidth,
      (Margin * 2f) + TitleRowHeight + Gap + StepRowHeight + Gap + ButtonRowHeight);

  // Verse.Text measures line heights off the loaded font at startup, so there is no
  // constant to hardcode. Both rows fall back to one line until a draw measures them.
  private float TitleRowHeight => titleHeight > 0f ? titleHeight : Text.LineHeightOf(GameFont.Small);

  private float StepRowHeight => stepHeight > 0f ? stepHeight : Text.LineHeightOf(GameFont.Tiny);

  // Window.PreOpen() always centers via InitialSize; overriding this is the only
  // way to land the pill top-right instead, matching the approved mock.

  /// <inheritdoc/>
  // Both labels wrap, so the pill is only as tall as the text currently needs.
  public override void WindowUpdate() {
    base.WindowUpdate();

    Vector2 size = InitialSize;
    if (!Mathf.Approximately(windowRect.height, size.y)) {
      windowRect = TopRight(size);
    }
  }

  /// <inheritdoc/>
  public override void DoWindowContents(Rect inRect) {
    RunSession? session = owner.ActiveSession;
    bool paused = session?.IsPaused ?? false;

    // CalcHeight measures in the current font, so each label sets its own before asking.
    Text.Font = GameFont.Small;
    float y = inRect.y;
    float textWidth = inRect.width - TextIndent;

    // Centred on the first line, not the whole block, or a wrapped title drags the dot
    // down into the middle of the paragraph.
    Color dotColor = paused ? RunnerStatusColors.Paused : RunnerStatusColors.Passed;
    RunnerStatusColors.DrawDot(
        new Vector2(inRect.x + 6f, y + (Text.LineHeightOf(GameFont.Small) / 2f)), dotColor, 8f);

    string scenarioName = session?.CurrentScenarioName ?? string.Empty;
    string title = paused
        ? "Pickle_PillPaused".Translate(scenarioName).ToString()
        : "Pickle_PillRunning".Translate(scenarioName).ToString();
    GUI.color = paused ? RunnerStatusColors.FailedText : Color.white;

    // Widgets.Label wraps on its own; the height it needs is what the pill grows to.
    titleHeight = Mathf.Max(Text.LineHeightOf(GameFont.Small), Text.CalcHeight(title, textWidth));
    Widgets.Label(new Rect(inRect.x + TextIndent, y, textWidth, titleHeight), title);
    GUI.color = Color.white;
    y += titleHeight + Gap;

    Text.Font = GameFont.Tiny;
    GUI.color = RunnerStatusColors.Muted;
    string step = session?.CurrentStepDisplay ?? string.Empty;

    stepHeight = Mathf.Max(Text.LineHeightOf(GameFont.Tiny), Text.CalcHeight(step, textWidth));
    Widgets.Label(new Rect(inRect.x + TextIndent, y, textWidth, stepHeight), step);
    GUI.color = Color.white;
    Text.Font = GameFont.Small;
    y += stepHeight + Gap;

    Rect expandRect = new Rect(inRect.x, y, ButtonWidth, ButtonRowHeight);
    Rect abortRect = new Rect(expandRect.xMax + Gap, y, ButtonWidth, ButtonRowHeight);

    if (Widgets.ButtonText(expandRect, "Pickle_Expand".Translate())) {
      owner.ExpandFromPill();
    }

    if (Widgets.ButtonText(abortRect, "Pickle_Abort".Translate())) {
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

  /// <inheritdoc/>
  protected override void SetInitialSizeAndPosition() {
    windowRect = TopRight(InitialSize);
  }

  private static Rect TopRight(Vector2 size) {
    return new Rect(Verse.UI.screenWidth - size.x - ScreenInset, ScreenInset, size.x, size.y);
  }
}
