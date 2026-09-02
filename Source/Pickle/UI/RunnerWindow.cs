using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Gherkin;
using Gherkin.Ast;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Run;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.Web;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Two-pane test runner: mod, feature and scenario tree on the left, the selected
/// scenario's steps and failure evidence on the right.
/// </summary>
public class RunnerWindow : Window {
  private const float ToolbarHeight = 34f;
  private const float PaneTopPadding = 10f;
  private const float PaneGutter = 14f;

  private const float FilterRowHeight = 30f;
  private const float StatusRowHeight = 24f;

  // Long enough that a human at a break card never times it out. CancelRequested is
  // the other way out and fires regardless.
  private const float BreakWaitTimeoutSeconds = 3600f;
  private readonly List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures = [];
  private readonly Dictionary<(string SourcePath, int ScenarioIndex), ScenarioResult> results = [];

  // Deselected, not selected: a scenario is on the moment it is discovered, with no
  // backfill on reparse. Mod and feature checkboxes derive from their children.
  private readonly HashSet<(string SourcePath, int ScenarioIndex)> deselectedScenarios = [];
  private RunPill? activePill;
  private bool restoreWindowAfterRun;

  public RunnerWindow() {
    optionalTitle = "Pickle_RunnerTitle".Translate();
    draggable = true;
    resizeable = true;
    doCloseX = true;
    closeOnClickedOutside = false;

    PickleDriver.EnsureExists();
    DiscoverAndParseFeatures();
  }

  // PreOpen always rebuilds windowRect from this property, overwriting anything the
  // constructor set, so the size has to live here. 0.8 leaves a tenth of the screen
  // either side.

  /// <inheritdoc/>
  public override Vector2 InitialSize =>
      new Vector2(Verse.UI.screenWidth * 0.8f, Verse.UI.screenHeight * 0.8f);

  // One runner per session, so a run started in the browser shows up in game. It exists
  // whether or not the window was ever opened.
  internal static RunnerWindow Instance => field ??= new RunnerWindow();

  internal bool IsRunning { get; private set; }

  internal IReadOnlyList<(DiscoveredSuite Suite, FeaturePlan Plan)> ParsedFeatures => parsedFeatures;

  internal Vector2 TreeScroll { get; set; }

  internal Vector2 DetailScroll { get; set; }

  internal bool HasAnyScenarioSelected {
    get {
      int index = 0;
      foreach ((DiscoveredSuite _, FeaturePlan plan) in parsedFeatures) {
        string sourcePath = plan.SourcePath ?? string.Empty;
        for (int i = 0; i < plan.Scenarios.Count; i++) {
          if (IsScenarioSelected(sourcePath, index + i)) {
            return true;
          }
        }

        index += plan.Scenarios.Count;
      }

      return false;
    }
  }

  internal RunSession? ActiveSession { get; private set; }

  internal int TotalScenarioCount => parsedFeatures.Sum(f => f.Plan.Scenarios.Count);

  internal DateTime? LastRunAt { get; private set; }

  internal string SearchText { get; set => field = value ?? string.Empty; } = string.Empty;

  internal HashSet<string> ActiveTagFilters { get; } = [];

  internal string? ModFilterSelection { get; set; }

  internal List<DiscoveredSuite> DiscoveredSuites { get; private set; } = [];

  internal IEnumerable<string> AllModNames => DiscoveredSuites.Select(s => s.ModName).Distinct();

  internal IEnumerable<string> AllTags => parsedFeatures
      .SelectMany(f => f.Plan.Scenarios)
      .SelectMany(s => s.Tags)
      .Distinct()
      .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

  internal (string SourcePath, int ScenarioIndex)? Selected { get; set; }

  // Preserved for RunnerWindowSmoke.cs, which relies on a full unconditional run.
  internal int ParsedFeaturesCount => parsedFeatures.Count;

  internal int FailedResultsCount => results.Values.Count(r => r.Outcome == ScenarioOutcome.Failed);

  /// <inheritdoc/>
  public override void DoWindowContents(Rect inRect) {
    float y = inRect.y;

    Rect toolbarRect = new Rect(inRect.x, y, inRect.width, ToolbarHeight);
    RunnerToolbar.Draw(toolbarRect, this);
    y += ToolbarHeight;
    Widgets.DrawLineHorizontal(inRect.x, y, inRect.width, Widgets.SeparatorLineColor);

    Rect filterRect = new Rect(inRect.x, y, inRect.width, FilterRowHeight);
    RunnerFilterBar.Draw(filterRect, this);
    y += FilterRowHeight;
    Widgets.DrawLineHorizontal(inRect.x, y, inRect.width, Widgets.SeparatorLineColor);

    float bodyHeight = inRect.height - (y - inRect.y) - StatusRowHeight;
    Rect bodyRect = new Rect(inRect.x, y, inRect.width, bodyHeight);

    float treeWidth = Mathf.Clamp(bodyRect.width * 0.4f, 220f, 420f);
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

    y += bodyHeight;
    Rect statusRect = new Rect(inRect.x, y, inRect.width, StatusRowHeight);
    DrawStatusRow(statusRect);
  }

  internal bool TryGetResult(string sourcePath, int scenarioIndex, out ScenarioResult result) {
    return results.TryGetValue((sourcePath, scenarioIndex), out result!);
  }

  internal bool TryGetSelectedScenario(out DiscoveredSuite suite, out FeaturePlan plan, out ScenarioPlan scenario, out int scenarioIndex) {
    suite = null!;
    plan = null!;
    scenario = null!;
    scenarioIndex = -1;

    if (Selected == null) {
      return false;
    }

    (string sourcePath, int index) = Selected.Value;
    int running = 0;

    foreach ((DiscoveredSuite candidateSuite, FeaturePlan candidatePlan) in parsedFeatures) {
      int count = candidatePlan.Scenarios.Count;
      bool inRange = index >= running && index < running + count;
      if (inRange && (candidatePlan.SourcePath ?? string.Empty) == sourcePath) {
        suite = candidateSuite;
        plan = candidatePlan;
        scenario = candidatePlan.Scenarios[index - running];
        scenarioIndex = index;
        return true;
      }

      running += count;
    }

    return false;
  }

  internal bool IsScenarioSelected(string sourcePath, int scenarioIndex) {
    return !deselectedScenarios.Contains((sourcePath, scenarioIndex));
  }

  internal bool IsScenarioVisible(DiscoveredSuite suite, FeaturePlan plan, ScenarioPlan scenario) {
    if (ModFilterSelection != null && suite.ModName != ModFilterSelection) {
      return false;
    }

    if (ActiveTagFilters.Count > 0 && !ActiveTagFilters.All(t => scenario.Tags.Contains(t))) {
      return false;
    }

    if (string.IsNullOrEmpty(SearchText)) {
      return true;
    }

    return scenario.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0
        || plan.Name.IndexOf(SearchText, StringComparison.OrdinalIgnoreCase) >= 0;
  }

  // Picking a tag is how you say "run these", so the filter becomes the selection outright
  // rather than leaving whatever was ticked before it.
  internal void SelectOnlyVisible() {
    int index = 0;
    foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
      string sourcePath = plan.SourcePath ?? string.Empty;
      for (int i = 0; i < plan.Scenarios.Count; i++) {
        SetScenarioSelected(sourcePath, index + i, IsScenarioVisible(suite, plan, plan.Scenarios[i]));
      }

      index += plan.Scenarios.Count;
    }
  }

  internal void SetScenarioSelected(string sourcePath, int scenarioIndex, bool selected) {
    (string, int) key = (sourcePath, scenarioIndex);
    if (selected) {
      deselectedScenarios.Remove(key);
    } else {
      deselectedScenarios.Add(key);
    }
  }

  internal void PublishSnapshot() {
    PickleHttpServer.Publish(
        RunnerSnapshot.Build(parsedFeatures, results, ActiveSession, IsRunning, IsScenarioSelected));
  }

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

  internal Task RunAllAndWait() {
    return RunAsync(null);
  }

  internal void RunSelected() {
    _ = RunAsync(IsScenarioSelected);
  }

  internal void RerunFailed() {
    HashSet<(string SourcePath, int ScenarioIndex)> failedKeys = [.. results
        .Where(kv => kv.Value.Outcome == ScenarioOutcome.Failed)
        .Select(kv => kv.Key)];

    if (failedKeys.Count == 0) {
      return;
    }

    _ = RunAsync((sourcePath, index) => failedKeys.Contains((sourcePath, index)));
  }

  private static List<int> SelectedPositions(
      string sourcePath, int featureStartIndex, int scenarioCount, Func<string, int, bool>? isScenarioSelected) {
    List<int> positions = [];
    for (int i = 0; i < scenarioCount; i++) {
      if (isScenarioSelected == null || isScenarioSelected(sourcePath, featureStartIndex + i)) {
        positions.Add(i);
      }
    }

    return positions;
  }

  // RunFeature's filter sees no index, so this rebuilds one from call order. It is called
  // once per scenario in plan order, so position lines up.
  // Keyed on the plan, not call order. A counter drifts whenever the filter is called
  // more than once for a scenario, and past the end every index reads as selected.
  private static Func<ScenarioPlan, bool>? BuildScenarioFilter(
      string sourcePath,
      int featureStartIndex,
      IReadOnlyList<ScenarioPlan> scenarios,
      Func<string, int, bool>? isScenarioSelected) {
    if (isScenarioSelected == null) {
      return null;
    }

    Dictionary<ScenarioPlan, int> positions = new Dictionary<ScenarioPlan, int>();
    for (int i = 0; i < scenarios.Count; i++) {
      positions[scenarios[i]] = i;
    }

    return scenario =>
        positions.TryGetValue(scenario, out int position)
        && isScenarioSelected(sourcePath, featureStartIndex + position);
  }

  private static Assembly? FindVanillaAssembly() {
    Type? vanillaType = Type.GetType("RimWorks.Pickle.Vanilla.VanillaSteps, RimWorks.Pickle.Vanilla");
    if (vanillaType != null) {
      return vanillaType.Assembly;
    }

    return AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "RimWorks.Pickle.Vanilla");
  }

  private static void AddStepsDlls(List<Assembly> assemblies, DiscoveredSuite suite) {
    foreach (string stepsDll in suite.StepsDlls) {
      try {
        Assembly loadedAsm = Assembly.LoadFrom(stepsDll);
        if (!assemblies.Contains(loadedAsm)) {
          assemblies.Add(loadedAsm);
        }
      } catch (Exception ex) {
        Log.Error(ex, $"pickle: failed to load steps dll {stepsDll}");
      }
    }
  }

  // Reparsing keeps results, so a reload does not blank mods it never ran. A feature
  // whose scenario order changed can show a stale row until the next full run.
  private void DiscoverAndParseFeatures() {
    DiscoveredSuites = SuiteScanner.DiscoverSuites();
    parsedFeatures.Clear();

    foreach (DiscoveredSuite suite in DiscoveredSuites) {
      foreach (string featureFile in suite.FeatureFiles) {
        try {
          string featureText = File.ReadAllText(featureFile);
          StringReader reader = new StringReader(featureText);
          Parser parser = new Parser();
          GherkinDocument gherkinDoc = parser.Parse(reader);
          FeaturePlan plan = GherkinAdapter.Adapt(gherkinDoc, featureFile);
          parsedFeatures.Add((suite, plan));
        } catch (Exception ex) {
          Log.Error(ex, $"pickle: failed to parse {Path.GetFileName(featureFile)}");
        }
      }
    }

    PublishSnapshot();
  }

  private void DrawStatusRow(Rect rect) {
    Widgets.DrawLineHorizontal(rect.x, rect.y, rect.width, Widgets.SeparatorLineColor);

    int passed = results.Values.Count(r => r.Outcome == ScenarioOutcome.Passed);
    int failed = FailedResultsCount;
    int notRun = TotalScenarioCount - results.Count;

    Text.Font = GameFont.Tiny;
    Text.Anchor = TextAnchor.MiddleLeft;
    Widgets.Label(new Rect(rect.x + 2f, rect.y, 260f, rect.height), $"{passed} passed · {failed} failed · {notRun} not run");

    string modeText = PickleRunMode.Current == PickleRunMode.Mode.Watch ? "watch mode" : "fast mode";
    string lastRunText = LastRunAt.HasValue ? $"last run {LastRunAt.Value:HH:mm:ss} · {modeText}" : modeText;
    GUI.color = RunnerStatusColors.Muted;
    Widgets.Label(new Rect(rect.x + 270f, rect.y, 260f, rect.height), lastRunText);
    GUI.color = Color.white;

    Text.Anchor = TextAnchor.MiddleRight;
    Widgets.Label(new Rect(rect.xMax - 300f, rect.y, 296f, rect.height), "junit + messages written to pickle-reports/");

    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;
  }

  // Keyed by (sourcePath, global scenario index), same as RunnerTreeView and results.
  // Null runs everything; a feature with nothing selected is skipped outright.
  private async Task RunAsync(Func<string, int, bool>? isScenarioSelected) {
    if (IsRunning) {
      return;
    }

    IsRunning = true;
    try {
      DiscoverAndParseFeatures();

      List<Assembly> assemblies = BuildAssemblyList();
      StepTable stepTable = StepScanner.PopulateStepTable(assemblies);
      List<Type> stepsTypes = StepScanner.GetPickleStepsTypes(assemblies);

      RunSession session = new RunSession(stepTable, PickleDriver.Instance, DiscoveredSuites, stepsTypes);
      ActiveSession = session;
      session.OnBreak = HandleBreak;
      session.OnProgress = PublishSnapshot;
      PickleHttpServer.ActiveSession = session;
      PublishSnapshot();

      // Only restore what was on screen, or a dashboard run pops the window open at the end.
      // WindowStack is null until a UIRoot exists, which a headless run precedes.
      restoreWindowAfterRun = Find.WindowStack != null && Find.WindowStack.IsOpen(this);

      // The dashboard replaced the pill because it survives the scene reload each fixture
      // load triggers. Collapse only when one is serving, or there is no way to abort.
      if (PickleHttpServer.IsRunning) {
        Close(false);
      }

      int scenarioIndex = 0;
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
        if (session.CancelRequested) {
          break;
        }

        string sourcePath = plan.SourcePath ?? string.Empty;
        int featureStartIndex = scenarioIndex;
        int scenarioCount = plan.Scenarios.Count;

        List<int> selectedPositions = SelectedPositions(sourcePath, featureStartIndex, scenarioCount, isScenarioSelected);
        if (selectedPositions.Count == 0) {
          scenarioIndex += scenarioCount;
          continue;
        }

        Func<ScenarioPlan, bool>? scenarioFilter = BuildScenarioFilter(sourcePath, featureStartIndex, plan.Scenarios, isScenarioSelected);

        // Results land per scenario rather than per feature so the dashboard's
        // tree fills in live instead of a whole feature at a time.
        int completed = 0;
        await session.RunFeature(
            plan,
            suite.ModName,
            onScenarioCompleted: result => {
              if (completed < selectedPositions.Count) {
                results[(sourcePath, featureStartIndex + selectedPositions[completed])] = result;
                completed++;
              }

              PublishSnapshot();
            },
            scenarioFilter: scenarioFilter);

        PublishSnapshot();

        scenarioIndex += scenarioCount;
      }

      LastRunAt = DateTime.Now;
      SelectFirstFailureIfNoneSelected();

      // Back to the main menu so the next run starts clean. Break on failure means the world
      // the failure left is the thing you want to look at, so that case stays loaded.
      if (!BreakOnFailureState.Enabled && !session.CancelRequested
          && Current.ProgramState == ProgramState.Playing) {
        GenScene.GoToMainMenu();

        // Waited, not ExecuteWhenFinished: that fires before the menu scene swaps, so the
        // window it adds is wiped by the load that follows.
        try {
          await PickleDriver.Instance.WaitUntil(() => Current.ProgramState == ProgramState.Entry, 60f);
          RestoreAfterMainMenu();
        } catch (TimeoutException) {
          Log.Warn("pickle: the main menu never came up, so the runner window stayed closed");
        }
      }
    } catch (Exception ex) {
      Log.Error(ex, "pickle: runner window run failed");
    } finally {
      IsRunning = false;
      ActiveSession = null;
      PickleHttpServer.ActiveSession = null;
      PublishSnapshot();

      if (activePill != null) {
        Find.WindowStack.TryRemove(activePill, doCloseSound: false);
        activePill = null;
      }

      if (restoreWindowAfterRun && !Find.WindowStack.IsOpen(this)) {
        Find.WindowStack.Add(this);
      }
    }
  }

  // The scene change clears the window stack, so the runner is added back after it lands.
  private void RestoreAfterMainMenu() {
    if (restoreWindowAfterRun && Find.WindowStack != null && !Find.WindowStack.IsOpen(this)) {
      Find.WindowStack.Add(this);
    }
  }

  // Waits on the same pump as every other Pickle wait, not Task.Delay, so the main thread
  // keeps rendering the paused world while RunSession blocks.
  private async Task HandleBreak((string FeatureName, string ScenarioName, string? SourcePath, int ScenarioIndex, StepResult FailingStep) info) {
    BreakCard card = new BreakCard(info.FeatureName, info.ScenarioName, info.FailingStep);
    Find.WindowStack.Add(card);

    RunSession? session = ActiveSession;
    await PickleDriver.Instance.WaitUntil(
        () => card.Decision.HasValue || (session != null && session.CancelRequested),
        BreakWaitTimeoutSeconds);

    Find.WindowStack.TryRemove(card, doCloseSound: false);

    if (card.Decision == BreakCardDecision.OpenInResults) {
      Selected = (info.SourcePath ?? string.Empty, info.ScenarioIndex);
      session?.RequestCancel();
    } else if (card.Decision == BreakCardDecision.Abort) {
      session?.RequestCancel();
    }
  }

  private void SelectFirstFailureIfNoneSelected() {
    if (Selected != null) {
      return;
    }

    foreach (KeyValuePair<(string SourcePath, int ScenarioIndex), ScenarioResult> entry in results) {
      if (entry.Value.Outcome == ScenarioOutcome.Failed) {
        Selected = entry.Key;
        return;
      }
    }
  }

  private List<Assembly> BuildAssemblyList() {
    List<Assembly> assemblies = [typeof(RunnerWindow).Assembly];

    Assembly? vanilla = FindVanillaAssembly();
    if (vanilla != null) {
      assemblies.Add(vanilla);
    }

    foreach (DiscoveredSuite suite in DiscoveredSuites) {
      AddStepsDlls(assemblies, suite);
    }

    return assemblies;
  }
}
