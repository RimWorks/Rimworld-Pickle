using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using RimWorks.Pickle.Autorun;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Hosting;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.Web;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Run;

/// <summary>
/// The run state and run control behind the runner window and the dashboard: the parsed
/// features, their results, the filters, the selection, and the runs over them.
/// </summary>
public class RunController {
  private readonly List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures = [];
  private readonly Dictionary<(string SourcePath, int ScenarioIndex), ScenarioResult> results = [];
  private readonly HashSet<(string SourcePath, int ScenarioIndex)> deselectedScenarios = [];

  private HashSet<(string SourcePath, int ScenarioIndex)>? visibleScenarios;

  private RunController() {
    PickleDriver.EnsureExists();
    DiscoverAndParseFeatures();
  }

  /// <summary>The one run controller for this session. It discovers and parses every feature when first read.</summary>
  public static RunController Instance => field ??= new RunController();

  /// <summary>Called when a step fails with break-on-failure armed. The run stays paused until the
  /// returned <see cref="Task"/> completes. With nothing set, a failed step does not pause the run.</summary>
  public Func<(string FeatureName, string ScenarioName, string? SourcePath, int ScenarioIndex, StepResult FailingStep), Task>? OnBreak { get; set; }

  /// <summary>Called once a run has started its session, before the first feature runs.</summary>
  public Action? OnRunStarted { get; set; }

  /// <summary>Called once a run has ended and its state is cleared, whether it finished, was cancelled, or threw.</summary>
  public Action? OnRunEnded { get; set; }

  /// <summary>Called when <see cref="ContinueRun"/> asks a paused run to go on.</summary>
  public Action? OnContinue { get; set; }

  /// <summary>Called when following a run moves <see cref="Selected"/> to a different scenario.</summary>
  public Action? OnSelectionFollowed { get; set; }

  /// <summary>Whether a suite is currently running through this controller.</summary>
  public bool IsRunning { get; private set; }

  /// <summary>Every discovered feature and its parsed plan, in discovery order.</summary>
  public IReadOnlyList<(DiscoveredSuite Suite, FeaturePlan Plan)> ParsedFeatures => parsedFeatures;

  /// <summary>The mod names collapsed in the tree.</summary>
  public HashSet<string> CollapsedMods { get; } = [];

  /// <summary>The feature source paths collapsed in the tree.</summary>
  public HashSet<string> CollapsedFeatures { get; } = [];

  /// <summary>Whether at least one currently visible scenario is selected to run.</summary>
  public bool HasAnyScenarioSelected {
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
  public RunSession? ActiveSession { get; private set; }

  /// <summary>Which scenarios the next run covers: <c>all</c> or <c>selected</c>.</summary>
  public string RunScope { get; set; } = "all";

  /// <summary>How many scenarios the current or most recent run covers.</summary>
  public int RunScenarioCount { get; private set; }

  /// <summary>How many of those scenarios have finished.</summary>
  public int CompletedScenarioCount { get; private set; }

  /// <summary>How many currently visible scenarios are selected to run.</summary>
  public int SelectedScenarioCount => VisibleScenarios.Count(key => !deselectedScenarios.Contains(key));

  /// <summary>How many recorded results passed.</summary>
  public int PassedResultsCount => results.Values.Count(r => r.Outcome == ScenarioOutcome.Passed);

  /// <summary>How many recorded results were skipped.</summary>
  public int SkippedResultsCount => results.Values.Count(r => r.Outcome == ScenarioOutcome.Skipped);

  /// <summary>How many scenarios exist across every parsed feature.</summary>
  public int TotalScenarioCount => parsedFeatures.Sum(f => f.Plan.Scenarios.Count);

  /// <summary>When the most recent run finished, or <c>null</c> before the first one.</summary>
  public DateTime? LastRunAt { get; private set; }

  /// <summary>The tree's search filter text. A <c>null</c> assignment clears it instead of storing a null.</summary>
  public string SearchText { get; set => field = value ?? string.Empty; } = string.Empty;

  /// <summary>The tags the tree is currently filtered to.</summary>
  public HashSet<string> ActiveTagFilters { get; } = [];

  /// <summary>The single mod the tree is filtered to, or <c>null</c> for every mod.</summary>
  public string? ModFilterSelection { get; set; }

  /// <summary>Every mod suite found on disk, refreshed on each parse.</summary>
  public List<DiscoveredSuite> DiscoveredSuites { get; private set; } = [];

  /// <summary>Every discovered mod's name, without duplicates.</summary>
  public IEnumerable<string> AllModNames => DiscoveredSuites.Select(s => s.ModName).Distinct();

  /// <summary>Every tag used by any parsed scenario, without duplicates, sorted for the filter list.</summary>
  public IEnumerable<string> AllTags => parsedFeatures
      .SelectMany(f => f.Plan.Scenarios)
      .SelectMany(s => s.Tags)
      .Distinct()
      .OrderBy(t => t, StringComparer.OrdinalIgnoreCase);

  /// <summary>The scenario shown in the detail pane, keyed by its feature's source path and its scenario index.</summary>
  public (string SourcePath, int ScenarioIndex)? Selected { get; set; }

  /// <summary>Whether the tree selection jumps to follow the scenario currently running.</summary>
  public bool FollowRun { get; set; } = true;

  /// <summary>Whether a search, tag, or mod filter is narrowing the tree.</summary>
  public bool HasActiveFilter =>
      ActiveTagFilters.Count > 0 || !string.IsNullOrEmpty(SearchText) || ModFilterSelection != null;

  /// <summary>Scenario keys the current filters keep visible, computed once and cached until a filter changes.</summary>
  public HashSet<(string SourcePath, int ScenarioIndex)> VisibleScenarios {
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
  public int ParsedFeaturesCount => parsedFeatures.Count;

  /// <summary>How many recorded results failed.</summary>
  public int FailedResultsCount => results.Values.Count(r => r.Outcome == ScenarioOutcome.Failed);

  /// <summary>Looks up a scenario's recorded result.</summary>
  /// <param name="sourcePath">The feature file the scenario belongs to.</param>
  /// <param name="scenarioIndex">The scenario's index within the run.</param>
  /// <param name="result">The result, when one was found.</param>
  /// <returns><c>true</c> when a result was recorded for this scenario.</returns>
  public bool TryGetResult(string sourcePath, int scenarioIndex, out ScenarioResult result) {
    return results.TryGetValue((sourcePath, scenarioIndex), out result!);
  }

  /// <summary>Resolves the currently selected scenario back to its suite, feature plan, and scenario plan.</summary>
  /// <param name="suite">The suite the selected scenario's feature belongs to.</param>
  /// <param name="plan">The feature plan the selected scenario belongs to.</param>
  /// <param name="scenario">The selected scenario itself.</param>
  /// <param name="scenarioIndex">The scenario's global index across every parsed feature.</param>
  /// <returns><c>true</c> when a scenario is selected and still present in the parsed features.</returns>
  public bool TryGetSelectedScenario(out DiscoveredSuite suite, out FeaturePlan plan, out ScenarioPlan scenario, out int scenarioIndex) {
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
  public bool IsScenarioSelected(string sourcePath, int scenarioIndex) {
    (string, int) key = (sourcePath, scenarioIndex);
    return VisibleScenarios.Contains(key) && !deselectedScenarios.Contains(key);
  }

  /// <summary>Whether a scenario matches the current mod, tag, and search filters.</summary>
  /// <param name="suite">The suite the scenario's feature belongs to.</param>
  /// <param name="plan">The feature plan the scenario belongs to.</param>
  /// <param name="scenario">The scenario to test.</param>
  /// <returns><c>true</c> when the scenario should show in the tree.</returns>
  public bool IsScenarioVisible(DiscoveredSuite suite, FeaturePlan plan, ScenarioPlan scenario) {
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
  public void SetScenarioSelected(string sourcePath, int scenarioIndex, bool selected) {
    (string, int) key = (sourcePath, scenarioIndex);
    if (selected) {
      deselectedScenarios.Remove(key);
    } else {
      deselectedScenarios.Add(key);
    }

    RunScope = "selected";
  }

  /// <summary>
  /// Pushes the runner's current state to the dashboard, and, when following a run, moves
  /// the tree selection to the scenario now executing.
  /// </summary>
  public void PublishSnapshot() {
    if (FollowRun && ActiveSession?.CurrentScenario != null) {
      int index = 0;
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
        for (int i = 0; i < plan.Scenarios.Count; i++) {
          if (ReferenceEquals(plan.Scenarios[i], ActiveSession.CurrentScenario)) {
            (string, int) next = (plan.SourcePath ?? string.Empty, index + i);
            bool changed = Selected != next;
            Selected = next;
            if (changed) {
              OnSelectionFollowed?.Invoke();
            }

            CollapsedMods.Remove(suite.ModName);
            CollapsedFeatures.Remove(plan.SourcePath ?? string.Empty);
          }
        }

        index += plan.Scenarios.Count;
      }
    }

    PickleHttpServer.PublishSafely(
        () => RunnerSnapshot.Build(
            parsedFeatures, results, ActiveSession, IsRunning, IsScenarioSelected, this));
  }

  /// <summary>Resumes a paused run and answers a pending break with Continue.</summary>
  public void ContinueRun() {
    ActiveSession?.Resume();
    OnContinue?.Invoke();
  }

  /// <summary>Updates the tree's search, mod, and tag filters, and narrows the run scope to match a non-empty filter.</summary>
  /// <param name="search">The search text to apply, or <c>null</c> to leave it unchanged.</param>
  /// <param name="mod">The mod name to filter to, or <c>null</c> to leave it unchanged. An empty string clears it.</param>
  /// <param name="tag">A tag to toggle, or <c>null</c> to leave the tag filters unchanged. Ignored while a run is in progress.</param>
  /// <param name="additive">Whether <paramref name="tag"/> adds to the existing tag filters instead of replacing them.</param>
  /// <param name="clearTags">Whether to clear every tag filter before applying <paramref name="tag"/>.</param>
  public void SetFilter(string? search = null, string? mod = null, string? tag = null, bool additive = false, bool clearTags = false) {
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

    if (HasActiveFilter) {
      RunScope = "selected";
    }

    PublishSnapshot();
  }

  /// <summary>Runs every parsed scenario.</summary>
  /// <returns>A task that completes once the run has ended.</returns>
  public Task RunAllAndWait() {
    return RunAsync(null);
  }

  /// <summary>Starts a run over just the scenarios currently selected in the tree.</summary>
  public void RunSelected() {
    _ = RunAsync(IsScenarioSelected);
  }

  /// <summary>Starts a run over only the scenarios that failed last time. Does nothing when none did.</summary>
  public void RerunFailed() {
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

  private static async Task ReturnToMainMenu(RunSession session) {
    if (BreakOnFailureState.Enabled || session.CancelRequested
        || !HostRegistry.Get<IGameState>().IsInPlay) {
      return;
    }

    try {
      await HostRegistry.Get<ISaveGames>().ReturnToMainMenu();
    } catch (TimeoutException) {
      Log.WarnTo(PickleLog.Channel, "the main menu never came up, so the runner window stayed closed");
    }
  }

  private void DiscoverAndParseFeatures() {
    visibleScenarios = null;
    DiscoveredSuites = SuiteScanner.DiscoverSuites();
    parsedFeatures.Clear();

    parsedFeatures.AddRange(FeatureParser.ParseAll(DiscoveredSuites));

    PublishSnapshot();
  }

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
    List<Assembly> assemblies = SuiteRunner.BuildAssemblyList(DiscoveredSuites);
    RunSession session = new RunSession(
        StepScanner.PopulateStepTable(assemblies),
        PickleDriver.Instance,
        DiscoveredSuites,
        StepScanner.GetPickleStepsTypes(assemblies),
        runRetries: PickleArgs.Parse().Retries);

    ActiveSession = session;
    session.OnBreak = OnBreak;
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
      OnRunStarted?.Invoke();

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

      AutorunBootstrap.WriteReports(
          ScreenshotCapture.ReportRoot(),
          [.. results.Values],
          session.CancelRequested ? "cancelled" : "completed");

      await ReturnToMainMenu(session);
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "runner window run failed");
    } finally {
      IsRunning = false;
      ActiveSession = null;
      PickleHttpServer.ActiveSession = null;
      PublishSnapshot();
      OnRunEnded?.Invoke();
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
}
