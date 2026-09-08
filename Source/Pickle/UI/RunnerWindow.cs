using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Gherkin.Ast;
using RimWorks.Pickle.Autorun;
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
  private const float PaneTopPadding = 10f;
  private const float PaneGutter = 14f;

  // Long enough that a human at a break card never times it out. CancelRequested is
  // the other way out and fires regardless.
  private const float BreakWaitTimeoutSeconds = 3600f;
  private readonly List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures = [];
  private readonly Dictionary<(string SourcePath, int ScenarioIndex), ScenarioResult> results = [];

  // Deselected, not selected: a scenario is on the moment it is discovered, with no
  // backfill on reparse. Mod and feature checkboxes derive from their children.
  // Only explicit toggles live here. Visibility is applied on top, so widening a search
  // brings scenarios back instead of needing them re-ticked.
  private readonly HashSet<(string SourcePath, int ScenarioIndex)> deselectedScenarios = [];

  private HashSet<(string SourcePath, int ScenarioIndex)>? visibleScenarios;
  private RunPill? activePill;
  private bool restoreWindowAfterRun;
  private BreakCard? activeBreakCard;
  private FixtureManagerDialog? fixturesView;

  /// <summary>
  /// Initializes a new instance. Discovers and parses every feature immediately, so the
  /// tree is populated before the window ever draws.
  /// </summary>
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

  /// <summary>The one runner for this session, live whether or not its window is open.</summary>
  // One runner per session, so a run started in the browser shows up in game. It exists
  // whether or not the window was ever opened.
  internal static RunnerWindow Instance => field ??= new RunnerWindow();

  /// <summary>Whether a suite is currently running through this window.</summary>
  internal bool IsRunning { get; private set; }

  /// <summary>Every discovered feature and its parsed plan, in discovery order.</summary>
  internal IReadOnlyList<(DiscoveredSuite Suite, FeaturePlan Plan)> ParsedFeatures => parsedFeatures;

  /// <summary>The tree pane's scroll position.</summary>
  internal Vector2 TreeScroll { get; set; }

  /// <summary>The detail pane's scroll position, reset to zero whenever the selection changes.</summary>
  internal Vector2 DetailScroll { get; set; }

  /// <summary>The mod names collapsed in the tree.</summary>
  // Collapse is a view preference of the one runner instance, so it outlives a window
  // close the same way the selection and the results do.
  internal HashSet<string> CollapsedMods { get; } = [];

  /// <summary>The feature source paths collapsed in the tree.</summary>
  internal HashSet<string> CollapsedFeatures { get; } = [];

  /// <summary>Whether at least one currently visible scenario is selected to run.</summary>
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

  /// <summary>The session driving the run in progress, or <c>null</c> when nothing is running.</summary>
  internal RunSession? ActiveSession { get; private set; }

  /// <summary>Which top-level tab is showing: <c>run</c>, <c>fixtures</c>, or <c>reports</c>.</summary>
  internal string Workspace { get; set; } = "run";

  /// <summary>Which scenarios the next run covers: <c>all</c> or <c>selected</c>.</summary>
  internal string RunScope { get; set; } = "all";

  /// <summary>How many scenarios the current or most recent run covers.</summary>
  internal int RunScenarioCount { get; private set; }

  /// <summary>How many of those scenarios have finished.</summary>
  internal int CompletedScenarioCount { get; private set; }

  /// <summary>How many currently visible scenarios are selected to run.</summary>
  internal int SelectedScenarioCount => VisibleScenarios.Count(key => !deselectedScenarios.Contains(key));

  /// <summary>How many recorded results passed.</summary>
  internal int PassedResultsCount => results.Values.Count(r => r.Outcome == ScenarioOutcome.Passed);

  /// <summary>How many recorded results were skipped.</summary>
  internal int SkippedResultsCount => results.Values.Count(r => r.Outcome == ScenarioOutcome.Skipped);

  /// <summary>How many scenarios exist across every parsed feature.</summary>
  internal int TotalScenarioCount => parsedFeatures.Sum(f => f.Plan.Scenarios.Count);

  /// <summary>When the most recent run finished, or <c>null</c> before the first one.</summary>
  internal DateTime? LastRunAt { get; private set; }

  /// <summary>The tree's search filter text. A <c>null</c> assignment clears it instead of storing a null.</summary>
  internal string SearchText { get; set => field = value ?? string.Empty; } = string.Empty;

  /// <summary>The tags the tree is currently filtered to.</summary>
  internal HashSet<string> ActiveTagFilters { get; } = [];

  /// <summary>The single mod the tree is filtered to, or <c>null</c> for every mod.</summary>
  internal string? ModFilterSelection { get; set; }

  /// <summary>Every mod suite found on disk, refreshed on each parse.</summary>
  internal List<DiscoveredSuite> DiscoveredSuites { get; private set; } = [];

  /// <summary>Every discovered mod's name, without duplicates.</summary>
  internal IEnumerable<string> AllModNames => DiscoveredSuites.Select(s => s.ModName).Distinct();

  /// <summary>Every tag used by any parsed scenario, without duplicates, sorted for the filter list.</summary>
  internal IEnumerable<string> AllTags => parsedFeatures
      .SelectMany(f => f.Plan.Scenarios)
      .SelectMany(s => s.Tags)
      .Distinct()
      .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

  /// <summary>The scenario shown in the detail pane, keyed by its feature's source path and its scenario index.</summary>
  internal (string SourcePath, int ScenarioIndex)? Selected { get; set; }

  /// <summary>Whether the tree selection jumps to follow the scenario currently running.</summary>
  internal bool FollowRun { get; set; } = true;

  /// <summary>Whether a search, tag, or mod filter is narrowing the tree.</summary>
  internal bool HasActiveFilter =>
      ActiveTagFilters.Count > 0 || !string.IsNullOrEmpty(SearchText) || ModFilterSelection != null;

  /// <summary>Scenario keys the current filters keep visible, computed once and cached until a filter changes.</summary>
  internal HashSet<(string SourcePath, int ScenarioIndex)> VisibleScenarios {
    get {
      if (visibleScenarios != null) {
        return visibleScenarios;
      }

      visibleScenarios = [];
      int index = 0;
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
        string sourcePath = plan.SourcePath ?? string.Empty;
        for (int i = 0; i < plan.Scenarios.Count; i++) {
          if (IsScenarioVisible(suite, plan, plan.Scenarios[i])) {
            visibleScenarios.Add((sourcePath, index + i));
          }
        }

        index += plan.Scenarios.Count;
      }

      return visibleScenarios;
    }
  }

  /// <summary>How many features are currently parsed.</summary>
  // Preserved for RunnerWindowSmoke.cs, which relies on a full unconditional run.
  internal int ParsedFeaturesCount => parsedFeatures.Count;

  /// <summary>How many recorded results failed.</summary>
  internal int FailedResultsCount => results.Values.Count(r => r.Outcome == ScenarioOutcome.Failed);

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

  /// <summary>Looks up a scenario's recorded result.</summary>
  /// <param name="sourcePath">The feature file the scenario belongs to.</param>
  /// <param name="scenarioIndex">The scenario's index within the run.</param>
  /// <param name="result">The result, when one was found.</param>
  /// <returns><c>true</c> when a result was recorded for this scenario.</returns>
  internal bool TryGetResult(string sourcePath, int scenarioIndex, out ScenarioResult result) {
    return results.TryGetValue((sourcePath, scenarioIndex), out result!);
  }

  /// <summary>Resolves the currently selected scenario back to its suite, feature plan, and scenario plan.</summary>
  /// <param name="suite">The suite the selected scenario's feature belongs to.</param>
  /// <param name="plan">The feature plan the selected scenario belongs to.</param>
  /// <param name="scenario">The selected scenario itself.</param>
  /// <param name="scenarioIndex">The scenario's global index across every parsed feature.</param>
  /// <returns><c>true</c> when a scenario is selected and still present in the parsed features.</returns>
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

  /// <summary>Whether a scenario runs the next time the suite runs: visible under the current filters and not explicitly deselected.</summary>
  /// <param name="sourcePath">The feature file the scenario belongs to.</param>
  /// <param name="scenarioIndex">The scenario's global index across every parsed feature.</param>
  /// <returns><c>true</c> when the scenario is selected.</returns>
  internal bool IsScenarioSelected(string sourcePath, int scenarioIndex) {
    (string, int) key = (sourcePath, scenarioIndex);
    return VisibleScenarios.Contains(key) && !deselectedScenarios.Contains(key);
  }

  /// <summary>Whether a scenario matches the current mod, tag, and search filters.</summary>
  /// <param name="suite">The suite the scenario's feature belongs to.</param>
  /// <param name="plan">The feature plan the scenario belongs to.</param>
  /// <param name="scenario">The scenario to test.</param>
  /// <returns><c>true</c> when the scenario should show in the tree.</returns>
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

  /// <summary>Ticks or unticks a scenario's checkbox in the tree.</summary>
  /// <param name="sourcePath">The feature file the scenario belongs to.</param>
  /// <param name="scenarioIndex">The scenario's global index across every parsed feature.</param>
  /// <param name="selected">Whether the scenario should be selected to run.</param>
  internal void SetScenarioSelected(string sourcePath, int scenarioIndex, bool selected) {
    (string, int) key = (sourcePath, scenarioIndex);
    if (selected) {
      deselectedScenarios.Remove(key);
    } else {
      deselectedScenarios.Add(key);
    }
  }

  /// <summary>
  /// Pushes the runner's current state to the dashboard, and, when following a run, moves
  /// the tree selection to the scenario now executing.
  /// </summary>
  internal void PublishSnapshot() {
    if (FollowRun && ActiveSession?.CurrentScenario != null) {
      int index = 0;
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
        for (int i = 0; i < plan.Scenarios.Count; i++) {
          if (ReferenceEquals(plan.Scenarios[i], ActiveSession.CurrentScenario)) {
            (string, int) next = (plan.SourcePath ?? string.Empty, index + i);
            if (Selected != next) {
              DetailScroll = Vector2.zero;
            }

            Selected = next;
            CollapsedMods.Remove(suite.ModName);
            CollapsedFeatures.Remove(plan.SourcePath ?? string.Empty);
          }
        }

        index += plan.Scenarios.Count;
      }
    }

    PickleHttpServer.Publish(
        RunnerSnapshot.Build(parsedFeatures, results, ActiveSession, IsRunning, IsScenarioSelected, this));
  }

  /// <summary>Resumes a paused run and dismisses the break card, as if its Continue button was clicked.</summary>
  internal void ContinueRun() {
    ActiveSession?.Resume();
    if (activeBreakCard != null) {
      activeBreakCard.Decision = BreakCardDecision.Continue;
    }
  }

  /// <summary>Updates the tree's search, mod, and tag filters, and narrows the run scope to match a non-empty filter.</summary>
  /// <param name="search">The search text to apply, or <c>null</c> to leave it unchanged.</param>
  /// <param name="mod">The mod name to filter to, or <c>null</c> to leave it unchanged. An empty string clears it.</param>
  /// <param name="tag">A tag to toggle, or <c>null</c> to leave the tag filters unchanged. Ignored while a run is in progress.</param>
  /// <param name="additive">Whether <paramref name="tag"/> adds to the existing tag filters instead of replacing them.</param>
  /// <param name="clearTags">Whether to clear every tag filter before applying <paramref name="tag"/>.</param>
  internal void SetFilter(string? search = null, string? mod = null, string? tag = null, bool additive = false, bool clearTags = false) {
    if (search != null) {
      SearchText = search;
    }

    if (mod != null) {
      ModFilterSelection = mod.Length == 0 ? null : mod;
    }

    if (clearTags) {
      ActiveTagFilters.Clear();
    }

    if (tag != null && !IsRunning) {
      if (!additive) {
        ActiveTagFilters.Clear();
      }

      if (!additive || !ActiveTagFilters.Remove(tag)) {
        ActiveTagFilters.Add(tag);
      }
    }

    visibleScenarios = null;

    // A narrowed view means "run these", so the scope follows it. Leaving it on All read as
    // if the whole suite would run while the tree showed two scenarios.
    if (HasActiveFilter) {
      RunScope = "selected";
    }

    PublishSnapshot();
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

  /// <summary>Runs every parsed scenario.</summary>
  internal Task RunAllAndWait() {
    return RunAsync(null);
  }

  /// <summary>Starts a run over just the scenarios currently selected in the tree.</summary>
  internal void RunSelected() {
    _ = RunAsync(IsScenarioSelected);
  }

  /// <summary>Starts a run over only the scenarios that failed last time. Does nothing when none did.</summary>
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
        Log.ErrorTo(PickleLog.Channel, ex, $"failed to load steps dll {stepsDll}");
      }
    }
  }

  // Reparsing keeps results, so a reload does not blank mods it never ran. A feature
  // whose scenario order changed can show a stale row until the next full run.
  private void DiscoverAndParseFeatures() {
    visibleScenarios = null;
    DiscoveredSuites = SuiteScanner.DiscoverSuites();
    parsedFeatures.Clear();

    parsedFeatures.AddRange(FeatureParser.ParseAll(DiscoveredSuites));

    PublishSnapshot();
  }

  // Keyed by (sourcePath, global scenario index), same as RunnerTreeView and results.
  // Null runs everything; a feature with nothing selected is skipped outright.
  private void ClearPreviousResults(Func<string, int, bool>? isScenarioSelected) {
    RunScenarioCount = 0;
    CompletedScenarioCount = 0;
    int clearIndex = 0;
    foreach ((DiscoveredSuite _, FeaturePlan plan) in parsedFeatures) {
      string path = plan.SourcePath ?? string.Empty;
      foreach (int position in SelectedPositions(path, clearIndex, plan.Scenarios.Count, isScenarioSelected)) {
        results.Remove((path, clearIndex + position));
        RunScenarioCount++;
      }

      clearIndex += plan.Scenarios.Count;
    }
  }

  // Results land per scenario rather than per feature so the dashboard's tree fills in
  // live instead of a whole feature at a time.
  private async Task RunOneFeature(
      RunSession session,
      DiscoveredSuite suite,
      FeaturePlan plan,
      int featureStartIndex,
      Func<string, int, bool>? isScenarioSelected) {
    string sourcePath = plan.SourcePath ?? string.Empty;
    List<int> selectedPositions = SelectedPositions(sourcePath, featureStartIndex, plan.Scenarios.Count, isScenarioSelected);
    if (selectedPositions.Count == 0) {
      return;
    }

    Func<ScenarioPlan, bool>? scenarioFilter = BuildScenarioFilter(sourcePath, featureStartIndex, plan.Scenarios, isScenarioSelected);
    int completed = 0;
    await session.RunFeature(
        plan,
        suite.ModName,
        IncludeWipState.Enabled,
        onScenarioCompleted: result => {
          if (completed < selectedPositions.Count) {
            results[(sourcePath, featureStartIndex + selectedPositions[completed])] = result;
            completed++;
            CompletedScenarioCount++;
          }

          PublishSnapshot();
        },
        scenarioFilter: scenarioFilter);

    PublishSnapshot();
  }

  private RunSession StartSession() {
    List<Assembly> assemblies = BuildAssemblyList();
    RunSession session = new RunSession(
        StepScanner.PopulateStepTable(assemblies),
        PickleDriver.Instance,
        DiscoveredSuites,
        StepScanner.GetPickleStepsTypes(assemblies),
        runRetries: PickleArgs.Parse().Retries);

    ActiveSession = session;
    session.OnBreak = HandleBreak;
    session.OnProgress = PublishSnapshot;
    PickleHttpServer.ActiveSession = session;
    PublishSnapshot();
    return session;
  }

  private async Task RunAsync(Func<string, int, bool>? isScenarioSelected) {
    if (IsRunning || FixtureCommands.IsBusy) {
      return;
    }

    IsRunning = true;
    FollowRun = true;
    try {
      DiscoverAndParseFeatures();

      ClearPreviousResults(isScenarioSelected);

      RunSession session = StartSession();

      // Only restore what was on screen, or a dashboard run pops the window open at the end.
      // WindowStack is null until a UIRoot exists, which a headless run precedes.
      restoreWindowAfterRun = Find.WindowStack != null && Find.WindowStack.IsOpen(this);

      if (restoreWindowAfterRun) {
        Close(false);
      }

      // Built for every run, not just one started from the window. A dashboard run closes
      // nothing, so gating the pill on that left it with no in-game progress at all.
      if (Find.WindowStack != null) {
        activePill = new RunPill(this);
        PickleDriver.Instance.AddFrameHook(RestorePill);
        RestorePill();
      }

      int scenarioIndex = 0;
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
        if (session.CancelRequested) {
          break;
        }

        await RunOneFeature(session, suite, plan, scenarioIndex, isScenarioSelected);
        scenarioIndex += plan.Scenarios.Count;
      }

      LastRunAt = DateTime.Now;
      if (FollowRun) {
        Selected = null;
      }

      SelectFirstFailureIfNoneSelected();

      // Only autorun wrote reports before, so the status bar's promise was false for a run
      // started from the window or the dashboard. The dashboard opens this one when it lands.
      AutorunBootstrap.WriteReports(
          ScreenshotCapture.ReportRoot(),
          [.. results.Values],
          session.CancelRequested ? "cancelled" : "completed");

      await ReturnToMainMenu(session);
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "runner window run failed");
    } finally {
      PickleDriver.Instance.RemoveFrameHook(RestorePill);
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

  // Back to the main menu so the next run starts clean. Break on failure means the world
  // the failure left is the thing you want to look at, so that case stays loaded.
  private async Task ReturnToMainMenu(RunSession session) {
    if (BreakOnFailureState.Enabled || session.CancelRequested
        || Current.ProgramState != ProgramState.Playing) {
      return;
    }

    GenScene.GoToMainMenu();

    // Waited, not ExecuteWhenFinished: that fires before the menu scene swaps, so the
    // window it adds is wiped by the load that follows.
    try {
      await PickleDriver.Instance.WaitUntil(() => Current.ProgramState == ProgramState.Entry, 60f);
      RestoreAfterMainMenu();
    } catch (TimeoutException) {
      Log.WarnTo(PickleLog.Channel, "the main menu never came up, so the runner window stayed closed");
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
    activeBreakCard = card;
    Find.WindowStack.Add(card);

    RunSession? session = ActiveSession;
    await PickleDriver.Instance.WaitUntil(
        () => card.Decision.HasValue || (session != null && session.CancelRequested),
        BreakWaitTimeoutSeconds);

    Find.WindowStack.TryRemove(card, doCloseSound: false);

    if (card.Decision == BreakCardDecision.Abort) {
      session?.RequestCancel();
    }

    activeBreakCard = null;
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
