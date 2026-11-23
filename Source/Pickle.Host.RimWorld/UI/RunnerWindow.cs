using System.Threading.Tasks;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Run;
using RimWorks.Pickle.Runtime;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Two-pane test runner: mod, feature and scenario tree on the left, the selected
/// scenario's steps and failure evidence on the right.
/// </summary>
public class RunnerWindow : PickleWindow {
  private const float PaneTopPadding = 10f;
  private const float PaneGutter = 14f;

  // Long enough that a human at a break card never times it out. CancelRequested is
  // the other way out and fires regardless.
  private const float BreakWaitTimeoutSeconds = 3600f;
  private RunPill? activePill;
  private bool restoreWindowAfterRun;
  private BreakCard? activeBreakCard;
  private FixtureManagerDialog? fixturesView;

  /// <summary>Initializes a new instance.</summary>
  public RunnerWindow() {
    draggable = true;
    resizeable = true;
    doCloseX = true;
    closeOnClickedOutside = false;
  }

  // PreOpen always rebuilds windowRect from this property, overwriting anything the
  // constructor set, so the size has to live here. 0.8 leaves a tenth of the screen
  // either side.

  /// <inheritdoc/>
  public override Vector2 InitialSize =>
      new Vector2(Verse.UI.screenWidth * 0.8f, Verse.UI.screenHeight * 0.8f);

  /// <summary>The one runner for this session, live whether or not its window is open.</summary>
  // One runner per session, so a run started in the browser shows up in game. It exists
  // whether or not the window was ever opened.
  internal static RunnerWindow Instance {
    get {
      if (field == null) {
        field = new RunnerWindow();
        field.Attach();
      }

      return field;
    }
  }

#pragma warning disable SA1600, CS1591
  internal RunController Controller { get; } = RunController.Instance;
#pragma warning restore SA1600, CS1591

  /// <summary>The tree pane's scroll position.</summary>
  internal Vector2 TreeScroll { get; set; }

  /// <summary>The detail pane's scroll position, reset to zero whenever the selection changes.</summary>
  internal Vector2 DetailScroll { get; set; }

  /// <summary>Which top-level tab is showing: <c>run</c>, <c>fixtures</c>, or <c>reports</c>.</summary>
  internal string Workspace { get; set; } = "run";

  /// <inheritdoc/>
  public override void DoWindowContents(Rect inRect) {
    float y = inRect.y;

    float headerHeight = RunnerToolbar.HeaderHeight(inRect.width);
    RunnerToolbar.DrawHeader(new Rect(inRect.x, y, inRect.width, headerHeight), this);
    y += headerHeight;
    float toolbarHeight = RunnerToolbar.Height(inRect.width);
    Rect toolbarRect = new Rect(inRect.x, y, inRect.width, toolbarHeight);
    RunnerToolbar.Draw(toolbarRect, this);
    y += toolbarHeight;
    Widgets.DrawLineHorizontal(inRect.x, y, inRect.width, Widgets.SeparatorLineColor);

    float filterHeight = RunnerFilterBar.Height(inRect.width, this);
    Rect filterRect = new Rect(inRect.x, y, inRect.width, filterHeight);
    RunnerFilterBar.Draw(filterRect, this);
    y += filterHeight;
    RunnerToolbar.DrawProgress(new Rect(inRect.x, y, inRect.width, 3f), this);
    y += 3f;

    float bodyHeight = inRect.height - (y - inRect.y);
    Rect bodyRect = new Rect(inRect.x, y, inRect.width, bodyHeight);

    if (Workspace == "fixtures") {
      fixturesView ??= new FixtureManagerDialog();
      fixturesView.DoWindowContents(bodyRect.ContractedBy(12f));
      return;
    }

    if (Workspace == "reports") {
      RunnerToolbar.DrawReports(bodyRect.ContractedBy(16f), this);
      return;
    }

    float treeWidth = Mathf.Clamp(bodyRect.width * 0.3f, 180f, 420f);
    float dividerX = bodyRect.x + treeWidth;
    Widgets.DrawLineVertical(dividerX, bodyRect.y, bodyRect.height);

    // Both panes are inset from the separators above and beside them; without this
    // the first tree row and the detail title sit flush against the lines.
    Rect treeRect = new Rect(
        bodyRect.x,
        bodyRect.y + PaneTopPadding,
        treeWidth - PaneGutter,
        bodyRect.height - PaneTopPadding);
    Rect detailRect = new Rect(
        dividerX + PaneGutter,
        bodyRect.y + PaneTopPadding,
        bodyRect.width - treeWidth - PaneGutter,
        bodyRect.height - PaneTopPadding);

    RunnerTreeView.Draw(treeRect, this);
    RunnerDetailView.Draw(detailRect, this);
  }

  /// <summary>Closes the collapsed pill and reopens this same window.</summary>
  // Called by RunPill's expand button. Restoring the same instance (rather than
  // opening a new RunnerWindow) keeps the run's results/selection wired up as-is.
  internal void ExpandFromPill() {
    if (activePill != null) {
      Find.WindowStack.TryRemove(activePill, doCloseSound: false);
      activePill = null;
    }

    if (!Find.WindowStack.IsOpen(this)) {
      Find.WindowStack.Add(this);
    }
  }

  private void Attach() {
    Controller.OnRunStarted = CollapseToPill;
    Controller.OnRunEnded = RestoreAfterRun;
    Controller.OnBreak = HandleBreak;
    Controller.OnContinue = AnswerBreakWithContinue;
    Controller.OnSelectionFollowed = () => DetailScroll = Vector2.zero;
  }

  private void CollapseToPill() {
    restoreWindowAfterRun = Find.WindowStack != null && Find.WindowStack.IsOpen(this);

    if (restoreWindowAfterRun) {
      Close(false);
    }

    if (Find.WindowStack != null) {
      activePill = new RunPill(this);
      PickleDriver.Instance.AddFrameHook(RestorePill);
      RestorePill();
    }
  }

  private void RestoreAfterRun() {
    PickleDriver.Instance.RemoveFrameHook(RestorePill);

    if (activePill != null) {
      Find.WindowStack.TryRemove(activePill, doCloseSound: false);
      activePill = null;
    }

    if (restoreWindowAfterRun && Find.WindowStack != null && !Find.WindowStack.IsOpen(this)) {
      Find.WindowStack.Add(this);
    }
  }

  private void AnswerBreakWithContinue() {
    if (activeBreakCard != null) {
      activeBreakCard.Decision = BreakCardDecision.Continue;
    }
  }

  // Runs every frame, which is what re-adds the pill after the scene reload a fixture load
  // triggers, and what lets the toggle take effect part way through a run.
  private void RestorePill() {
    if (activePill == null || Find.WindowStack == null) {
      return;
    }

    bool open = Find.WindowStack.IsOpen(activePill);
    if (RunPillState.Enabled && !open) {
      Find.WindowStack.Add(activePill);
    } else if (!RunPillState.Enabled && open) {
      Find.WindowStack.TryRemove(activePill, doCloseSound: false);
    }
  }

  // Waits on the same pump as every other Pickle wait, not Task.Delay, so the main thread
  // keeps rendering the paused world while RunSession blocks.
  private async Task HandleBreak((string FeatureName, string ScenarioName, string? SourcePath, int ScenarioIndex, StepResult FailingStep) info) {
    BreakCard card = new BreakCard(info.FeatureName, info.ScenarioName, info.FailingStep);
    activeBreakCard = card;
    Find.WindowStack.Add(card);

    RunSession? session = Controller.ActiveSession;
    await PickleDriver.Instance.WaitUntil(
        () => card.Decision.HasValue || (session != null && session.CancelRequested),
        BreakWaitTimeoutSeconds);

    Find.WindowStack.TryRemove(card, doCloseSound: false);

    if (card.Decision == BreakCardDecision.Abort) {
      session?.RequestCancel();
    }

    activeBreakCard = null;
  }
}
