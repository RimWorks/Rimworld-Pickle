using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.ExceptionServices;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Pickle.Autorun;
using Pickle.Core;
using Pickle.Core.Discovery;
using Pickle.Core.Fixtures;
using Pickle.Core.Model;
using Pickle.Core.Run;
using Pickle.Core.Steps;
using Pickle.Evidence;
using Pickle.Fixtures;
using Pickle.Runtime;
using Verse;

namespace Pickle.Run;

public class RunSession {
  // Arbitrary fixed constant so a run with no -pickle-seed is still deterministic
  // run to run, matching what -pickle-seed=42 would produce.
  public const int DefaultSeed = 42;

  private const float FixtureStepTimeoutSeconds = 180f;

  private const string RoundTripSaveName = "__pickle_roundtrip";

  private const string WatchTag = "@watch";

  private readonly StepTable stepTable;
  private readonly PickleDriver driver;
  private readonly IReadOnlyList<DiscoveredSuite> suites;
  private readonly List<Type> stepsTypes;
  private readonly int runSeed;
  private readonly Dictionary<Type, object> scenarioInstanceCache = new();

  private List<StepResult> currentStepResults = new();
  private string? currentLoadedFixture;
  private string currentOwningMod = string.Empty;
  private TagSet currentScenarioTags = new TagSet(Array.Empty<string>());

  public RunSession(
      StepTable stepTable,
      PickleDriver driver,
      IReadOnlyList<DiscoveredSuite> suites,
      List<Type> stepsTypes,
      int runSeed = DefaultSeed) {
    this.stepTable = stepTable;
    this.driver = driver;
    this.suites = suites;
    this.stepsTypes = stepsTypes;
    this.runSeed = runSeed;

    RegisterBuiltInEngineSteps();
  }

  public string CurrentFeatureName { get; private set; } = string.Empty;

  public string CurrentScenarioName { get; private set; } = string.Empty;

  public string CurrentStepDisplay { get; private set; } = string.Empty;

  public int PassedCount { get; private set; }

  public int FailedCount { get; private set; }

  public bool IsPausedForBreak { get; private set; }

  public bool CancelRequested { get; private set; }

  // Fires when a step fails with break-on-failure armed; the run blocks on the Task.
  // The handler must wait via PickleDriver.WaitUntil or the main thread stops pumping.
  public Func<(string FeatureName, string ScenarioName, string? SourcePath, int ScenarioIndex, StepResult FailingStep), Task>? OnBreak { get; set; }

  // Fires on the main thread whenever the current step changes, so a watcher can
  // republish run state without polling live fields from another thread.
  public Action? OnProgress { get; set; }

  // Steps finished so far in the scenario currently running, so a watcher can show
  // per-step status before the scenario completes and produces a ScenarioResult.
  public IReadOnlyList<StepResult> CurrentStepResults => currentStepResults;

  public void RequestCancel() {
    CancelRequested = true;
  }

  public async Task<List<ScenarioResult>> RunFeature(
      FeaturePlan plan,
      string owningModName,
      bool includeWip = false,
      Action<ScenarioResult>? onScenarioCompleted = null,
      Func<ScenarioPlan, bool>? scenarioFilter = null) {
    List<ScenarioResult> results = new();
    currentOwningMod = owningModName;
    CurrentFeatureName = plan.Name;
    Stopwatch featureTimer = Stopwatch.StartNew();

    int scenarioIndex = 0;
    foreach (ScenarioPlan scenario in plan.Scenarios) {
      if (CancelRequested) {
        break;
      }

      // Deselected scenarios are not run and not reported at all - this is
      // distinct from @wip/@skip below, which still produce a Skipped result.
      if (scenarioFilter != null && !scenarioFilter(scenario)) {
        continue;
      }

      // @requires:<mod> keeps a dlc scenario out of a run that has no dlc, reported as
      // skipped rather than failed.
      string? missingMod = RunOutcomes.MissingRequirement(scenario.Tags, IsModPresent);
      if (missingMod != null) {
        Verse.Log.Message($"pickle: skipping '{scenario.Name}', '{missingMod}' is not loaded");
      }

      if (missingMod != null || RunOutcomes.ShouldSkip(scenario.Tags, includeWip)) {
        ScenarioResult skipped = new ScenarioResult(
            scenario.Name,
            plan.Name,
            scenario.Tags,
            ScenarioOutcome.Skipped,
            new List<StepResult>(),
            0);
        results.Add(skipped);
        onScenarioCompleted?.Invoke(skipped);
        scenarioIndex++;
        continue;
      }

      ScenarioResult result = await RunScenario(scenario, plan.Name, plan.SourcePath, scenarioIndex);
      results.Add(result);
      onScenarioCompleted?.Invoke(result);
      LogScenarioProgress(result);

      if (result.Outcome == ScenarioOutcome.Passed) {
        PassedCount++;
      } else if (result.Outcome == ScenarioOutcome.Failed) {
        FailedCount++;
      }

      scenarioIndex++;
    }

    featureTimer.Stop();

    int passedCount = results.Count(r => r.Outcome == ScenarioOutcome.Passed);
    int failedCount = results.Count(r => r.Outcome == ScenarioOutcome.Failed);
    int skippedCount = results.Count(r => r.Outcome == ScenarioOutcome.Skipped);

    Verse.Log.Message($"pickle: run finished: {passedCount} passed, {failedCount} failed, {skippedCount} skipped");

    return results;
  }

  // A headless run is silent for as long as it takes without this, so a watcher cannot
  // tell a slow scenario from a hung one.
  private static void LogScenarioProgress(ScenarioResult result) {
    string outcome = result.Outcome switch {
      ScenarioOutcome.Passed => "passed",
      ScenarioOutcome.Failed => "FAILED",
      _ => "skipped",
    };

    Verse.Log.Message(
        $"pickle: {outcome} in {result.DurationMs:F0}ms: {result.FeatureName}: {result.ScenarioName}");
  }

  private static Task InvokeDelegateBinding(PickleContext ctx, Delegate @delegate, IReadOnlyList<object?> args) {
    List<object?> allArgs = [ctx, .. args];
    object? result = @delegate.DynamicInvoke([.. allArgs]);

    if (result is Task task) {
      return task;
    }

    return Task.CompletedTask;
  }

  private static List<(string Name, string Content)> BuildAttachmentsWithScreenshot(
      PickleContext ctx, string? screenshotPath) {
    List<(string Name, string Content)> attachmentsWithScreenshot = [.. ctx.Attachments];

    if (screenshotPath is { Length: > 0 }) {
      attachmentsWithScreenshot.Add(("screenshot", screenshotPath));
    }

    return attachmentsWithScreenshot;
  }

  // Undefined and ambiguous beat a plain failure, because a missing step definition is
  // the more useful thing to report.
  private static string BuildFailureMessage(List<StepResult> stepResults) {
    string? message = RunOutcomes.BuildUndefinedMessage(stepResults);
    if (!string.IsNullOrEmpty(message)) {
      return message;
    }

    message = RunOutcomes.BuildAmbiguousMessage(stepResults);
    if (!string.IsNullOrEmpty(message)) {
      return message;
    }

    return stepResults.FirstOrDefault(s => s.Status == StepStatus.Failed)?.FailureMessage ?? "Scenario failed";
  }

  private static bool IsModPresent(string wanted) {
    foreach (Verse.ModContentPack pack in Verse.LoadedModManager.RunningModsListForReading) {
      if (string.Equals(pack.Name, wanted, StringComparison.OrdinalIgnoreCase)
          || string.Equals(pack.PackageId, wanted, StringComparison.OrdinalIgnoreCase)) {
        return true;
      }
    }

    return false;
  }

  private static void SkipRemainingSteps(ScenarioPlan scenario, List<StepResult> stepResults) {
    foreach (StepPlan remainingStep in scenario.Steps.Skip(stepResults.Count)) {
      stepResults.Add(new StepResult(remainingStep.Keyword, remainingStep.Text, StepStatus.Skipped, 0));
    }
  }

  private async Task<ScenarioResult> RunScenario(ScenarioPlan scenario, string featureName, string? sourcePath, int scenarioIndex) {
    Stopwatch scenarioTimer = Stopwatch.StartNew();
    Watchdog.BeginScenario(featureName, scenario.Name);
    PickleContext ctx = new PickleContext();
    currentScenarioTags = scenario.Tags;
    CurrentScenarioName = scenario.Name;
    scenarioInstanceCache.Clear();

    int scenarioSeed = runSeed;
    string? seedTag = scenario.Tags.FirstOrDefault(t => t.StartsWith("@seed:"));
    if (seedTag != null && int.TryParse(seedTag.Substring(6), out int parsedSeed)) {
      scenarioSeed = parsedSeed;
    }

    List<StepResult> stepResults = new();
    currentStepResults = stepResults;
    FilmstripRecorder? film = null;
    PickleRunMode.Mode modeBeforeScenario = PickleRunMode.Current;

    try {
      // Not Rand.PushState: Root.Update drains the stack whenever a step awaits past a
      // frame, so the matching PopState throws. Seed reseeds the same stream instead.
      Rand.Seed = scenarioSeed;
      LogWatch.Arm();

      await RunBeforeHooks(ctx, scenario.Tags);

      // no frame before the first step: a scenario usually opens by loading a fixture, so
      // a frame taken here shows the world the previous scenario left behind
      // Fast mode drives sixty ticks a frame, so a long wait costs almost no real time
      // and films almost nothing. @watch trades run time for a real recording.
      if (scenario.Tags.Contains(WatchTag)) {
        PickleRunMode.Current = PickleRunMode.Mode.Watch;
      }

      film = scenario.Tags.Contains(FilmstripRecorder.Tag)
          ? new FilmstripRecorder(ctx, featureName, scenario.Name)
          : null;
      film?.Start();

      foreach (StepPlan step in scenario.Steps) {
        StepResult stepResult = await RunStep(ctx, step);
        stepResults.Add(stepResult);
        OnProgress?.Invoke();

        if (RunOutcomes.EndsScenario(stepResult.Status)) {
          await MaybeBreakOnFailure(featureName, scenario.Name, sourcePath, scenarioIndex, stepResult);
          SkipRemainingSteps(scenario, stepResults);
          break;
        }

        if (LogWatch.Armed && LogWatch.ErrorCount > 0 && !scenario.Tags.Contains("@allow-errors")) {
          return await FailOnLoggedError(ctx, scenario, featureName, stepResults, scenarioTimer);
        }
      }

      await RunAfterHooks(ctx, scenario.Tags);

      scenarioTimer.Stop();

      ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(stepResults);
      string? failureMsg = null;

      if (outcome == ScenarioOutcome.Failed) {
        failureMsg = BuildFailureMessage(stepResults);

        int failingStepIndex = stepResults.FindIndex(s => s.Status == StepStatus.Failed) + 1;
        (string? screenshotPath, List<(string Source, string Content)> stateDumps) =
            await CaptureEvidence(featureName, scenario.Name, failingStepIndex);

        List<(string Name, string Content)> attachmentsWithScreenshot = BuildAttachmentsWithScreenshot(ctx, screenshotPath);

        return new ScenarioResult(
            scenario.Name,
            featureName,
            scenario.Tags,
            outcome,
            stepResults,
            scenarioTimer.ElapsedMilliseconds,
            failureMsg,
            LogWatch.ErrorsSinceArmed,
            attachmentsWithScreenshot,
            stateDumps);
      }

      return new ScenarioResult(
          scenario.Name,
          featureName,
          scenario.Tags,
          outcome,
          stepResults,
          scenarioTimer.ElapsedMilliseconds,
          failureMsg,
          LogWatch.ErrorsSinceArmed,
          ctx.Attachments);
    } catch (Exception ex) {
      scenarioTimer.Stop();

      int failingStepIndex = stepResults.Count + 1;
      (string? screenshotPath, List<(string Source, string Content)> stateDumps) =
          await CaptureEvidence(featureName, scenario.Name, failingStepIndex);

      foreach (StepPlan remainingStep in scenario.Steps.Skip(stepResults.Count)) {
        stepResults.Add(new StepResult(remainingStep.Keyword, remainingStep.Text, StepStatus.Skipped, 0));
      }

      List<(string Name, string Content)> attachmentsWithScreenshot = BuildAttachmentsWithScreenshot(ctx, screenshotPath);

      return new ScenarioResult(
          scenario.Name,
          featureName,
          scenario.Tags,
          ScenarioOutcome.Failed,
          stepResults,
          scenarioTimer.ElapsedMilliseconds,
          ex.Message,
          LogWatch.ErrorsSinceArmed,
          attachmentsWithScreenshot,
          stateDumps);
    } finally {
      film?.Finish();
      PickleRunMode.Current = modeBeforeScenario;
      LogWatch.Disarm();
      Watchdog.EndScenario();
    }
  }

  private async Task<StepResult> RunStep(PickleContext ctx, StepPlan stepPlan) {
    Stopwatch stepTimer = Stopwatch.StartNew();
    string stepDisplay = $"{stepPlan.Keyword.Trim()} {stepPlan.Text}";
    CurrentStepDisplay = stepDisplay;
    Watchdog.Heartbeat(stepDisplay);
    OnProgress?.Invoke();

    try {
      StepResolution resolution = stepTable.Resolve(stepPlan.Text);

      if (resolution is UndefinedStep undefinedStep) {
        stepTimer.Stop();
        return new StepResult(
            stepPlan.Keyword,
            stepPlan.Text,
            StepStatus.Undefined,
            stepTimer.ElapsedMilliseconds,
            undefinedStep.Skeleton);
      }

      if (resolution is AmbiguousStep ambiguousStep) {
        string sourcesList = string.Join(", ", ambiguousStep.Matches.Select(m => m.Source));
        stepTimer.Stop();
        return new StepResult(
            stepPlan.Keyword,
            stepPlan.Text,
            StepStatus.Ambiguous,
            stepTimer.ElapsedMilliseconds,
            $"Multiple matches found: {sourcesList}");
      }

      if (resolution is MatchedStep matchedStep) {
        StepDefinition definition = matchedStep.Definition;
        object? binding = definition.Binding;

        float timeout = ResolveStepTimeout(definition);

        object stepScope = new object();
        ctx.WaitScope = stepScope;
        Task stepTask;

        if (binding is MethodInfo method) {
          stepTask = InvokeMethodBinding(ctx, method, matchedStep.Args);
        } else if (binding is Delegate @delegate) {
          stepTask = InvokeDelegateBinding(ctx, @delegate, matchedStep.Args);
        } else {
          stepTimer.Stop();
          return new StepResult(
              stepPlan.Keyword,
              stepPlan.Text,
              StepStatus.Failed,
              stepTimer.ElapsedMilliseconds,
              "Invalid step binding");
        }

        try {
          await driver.WaitUntil(() => stepTask.IsCompleted, timeout, stepScope);
        } catch (TimeoutException) {
          string timeoutMessage = $"Step '{stepPlan.Keyword.Trim()} {stepPlan.Text}' timed out after {timeout}s";
          driver.FaultScope(stepScope, new TimeoutException(timeoutMessage));
          stepTimer.Stop();
          return new StepResult(
              stepPlan.Keyword,
              stepPlan.Text,
              StepStatus.Failed,
              stepTimer.ElapsedMilliseconds,
              timeoutMessage);
        }

        if (stepTask.IsFaulted) {
          Exception? ex = stepTask.Exception?.InnerException ?? stepTask.Exception;
          stepTimer.Stop();
          return new StepResult(
              stepPlan.Keyword,
              stepPlan.Text,
              StepStatus.Failed,
              stepTimer.ElapsedMilliseconds,
              ex?.Message ?? "Step execution failed");
        }

        stepTimer.Stop();
        return new StepResult(
            stepPlan.Keyword,
            stepPlan.Text,
            StepStatus.Passed,
            stepTimer.ElapsedMilliseconds);
      }

      stepTimer.Stop();
      return new StepResult(
          stepPlan.Keyword,
          stepPlan.Text,
          StepStatus.Failed,
          stepTimer.ElapsedMilliseconds,
          "Unknown step resolution");
    } catch (Exception ex) {
      stepTimer.Stop();
      return new StepResult(
          stepPlan.Keyword,
          stepPlan.Text,
          StepStatus.Failed,
          stepTimer.ElapsedMilliseconds,
          ex.Message);
    }
  }

  private Task InvokeMethodBinding(PickleContext ctx, MethodInfo method, IReadOnlyList<object?> args) {
    Type declaringType = method.DeclaringType!;
    object? instance = null;

    if (!method.IsStatic && !scenarioInstanceCache.TryGetValue(declaringType, out instance)) {
      instance = Activator.CreateInstance(declaringType)!;
      scenarioInstanceCache[declaringType] = instance;
    }

    List<object?> allArgs = new() { ctx };
    allArgs.AddRange(args);

    object? result;
    try {
      result = method.Invoke(instance, [.. allArgs]);
    } catch (TargetInvocationException invocationEx) when (invocationEx.InnerException != null) {
      // Reflection wraps whatever the step threw, so without unwrapping every failed
      // assert reads "Exception has been thrown by the target of an invocation".
      ExceptionDispatchInfo.Capture(invocationEx.InnerException).Throw();
      throw;
    }

    if (result is Task task) {
      return task;
    }

    return Task.CompletedTask;
  }

  private async Task RunBeforeHooks(PickleContext ctx, TagSet tags) {
    foreach (Type stepsType in stepsTypes) {
      MethodInfo[] methods = stepsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

      foreach (MethodInfo method in methods) {
        BeforeScenarioAttribute? beforeAttr = method.GetCustomAttribute<BeforeScenarioAttribute>();
        if (beforeAttr != null && (beforeAttr.Tag == null || tags.Contains(beforeAttr.Tag))) {
          await InvokeHook(ctx, stepsType, method);
        }
      }
    }
  }

  private async Task RunAfterHooks(PickleContext ctx, TagSet tags) {
    foreach (Type stepsType in stepsTypes) {
      MethodInfo[] methods = stepsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

      foreach (MethodInfo method in methods) {
        AfterScenarioAttribute? afterAttr = method.GetCustomAttribute<AfterScenarioAttribute>();
        if (afterAttr != null && (afterAttr.Tag == null || tags.Contains(afterAttr.Tag))) {
          await InvokeHook(ctx, stepsType, method);
        }
      }
    }
  }

  private async Task InvokeHook(PickleContext ctx, Type stepsType, MethodInfo method) {
    object? instance = null;

    if (!method.IsStatic && !scenarioInstanceCache.TryGetValue(stepsType, out instance)) {
      instance = Activator.CreateInstance(stepsType)!;
      scenarioInstanceCache[stepsType] = instance;
    }

    ParameterInfo[] parameters = method.GetParameters();
    object?[] hookArgs = [];

    if (parameters.Length > 0 && parameters[0].ParameterType == typeof(PickleContext)) {
      hookArgs = [ctx];
    }

    object? result = method.Invoke(instance, hookArgs);

    if (result is Task task) {
      await task;
    }
  }

  private void RegisterBuiltInEngineSteps() {
    AddEngineStep(
        "the save {string} is loaded",
        StepKind.Given,
        [typeof(string)],
        new Func<PickleContext, string, Task>(LoadFixtureStep));

    AddEngineStep(
        "I save and reload",
        StepKind.When,
        [],
        new Func<PickleContext, Task>(SaveAndReloadStep));

    AddEngineStep(
        "I save and reload as {string}",
        StepKind.When,
        [typeof(string)],
        new Func<PickleContext, string, Task>(SaveAndReloadAsStep));

    AddEngineStep(
        "the save round trips",
        StepKind.Then,
        [],
        new Func<PickleContext, Task>(RoundTripStep));
  }

  private void AddEngineStep(string pattern, StepKind kind, List<Type> parameterTypes, Delegate binding) {
    stepTable.Add(new StepDefinition(
        pattern,
        kind,
        "Pickle engine",
        parameterTypes,
        binding,
        FixtureStepTimeoutSeconds));
  }

  private Task SaveAndReloadStep(PickleContext ctx) {
    return FixtureLoader.SaveAndReload(RoundTripSaveName, driver, ctx.WaitScope, keepSave: false);
  }

  private Task SaveAndReloadAsStep(PickleContext ctx, string saveName) {
    return FixtureLoader.SaveAndReload(saveName, driver, ctx.WaitScope, keepSave: true);
  }

  // LogWatch is not re-armed around the trip, unlike a fixture load. An exception thrown
  // while the game writes or reads its own data is the bug this step exists to catch.
  private async Task RoundTripStep(PickleContext ctx) {
    long mark = LogWatch.Mark;
    await FixtureLoader.SaveAndReload(RoundTripSaveName, driver, ctx.WaitScope, keepSave: false);

    IReadOnlyList<string> errors = LogWatch.ErrorsSince(mark);
    ctx.Assert(
        errors.Count == 0,
        $"the save did not round trip; {errors.Count} error(s) logged: {string.Join(" | ", errors)}");
  }

  private async Task LoadFixtureStep(PickleContext ctx, string fixtureName) {
    if (currentScenarioTags.Contains("@same-world") && currentLoadedFixture == fixtureName) {
      return;
    }

    FixtureResolution resolution = FixtureResolver.Resolve(fixtureName, currentOwningMod, suites);

    if (resolution.Error != null) {
      throw new InvalidOperationException(resolution.Error.Message);
    }

    await FixtureLoader.LoadFixture(resolution.Fixture!.FullPath, driver, ctx.WaitScope);

    currentLoadedFixture = fixtureName;
    LogWatch.Arm();
  }

  private async Task<(string? ScreenshotPath, List<(string Source, string Content)> StateDumps)> CaptureEvidence(
      string featureName, string scenarioName, int stepIndex) {
    string? screenshotPath = null;
    List<(string Source, string Content)> stateDumps = [];

    try {
      screenshotPath = ScreenshotCapture.BuildScreenshotPath(featureName, scenarioName, stepIndex);
      await ScreenshotCapture.CaptureToFile(screenshotPath);

      List<KeyValuePair<Type, object>> instances = [.. scenarioInstanceCache];
      stateDumps = StateDumpCollector.Collect(instances);
    } catch (Exception ex) {
      Log.Warning($"pickle: error capturing evidence: {ex.Message}");
    }

    return (screenshotPath, stateDumps);
  }

  // The step attribute wins over an @timeout: tag, because a step that waits on the
  // simulation knows how long it needs better than the scenario does.
  private float ResolveStepTimeout(StepDefinition definition) {
    if (definition.TimeoutSeconds.HasValue) {
      return definition.TimeoutSeconds.Value;
    }

    string? timeoutTag = currentScenarioTags.FirstOrDefault(t => t.StartsWith("@timeout:"));
    if (timeoutTag != null && float.TryParse(timeoutTag.Substring(9), out float parsedTimeout)) {
      return parsedTimeout;
    }

    return 5f;
  }

  // An error logged mid-scenario ends it the same way a failed assertion does: capture
  // evidence, mark the rest skipped, still run the after hooks.
  private async Task<ScenarioResult> FailOnLoggedError(
      PickleContext ctx,
      ScenarioPlan scenario,
      string featureName,
      List<StepResult> stepResults,
      Stopwatch scenarioTimer) {
    IReadOnlyList<string> errorLog = LogWatch.ErrorsSinceArmed;
    string errorMsg = errorLog.Count > 0 ? errorLog[0] : "unknown error";
    scenarioTimer.Stop();

    (string? screenshotPath, List<(string Source, string Content)> stateDumps) =
        await CaptureEvidence(featureName, scenario.Name, stepResults.Count + 1);

    foreach (StepPlan remainingStep in scenario.Steps.Skip(stepResults.Count)) {
      stepResults.Add(new StepResult(remainingStep.Keyword, remainingStep.Text, StepStatus.Skipped, 0));
    }

    await RunAfterHooks(ctx, scenario.Tags);

    return new ScenarioResult(
        scenario.Name,
        featureName,
        scenario.Tags,
        ScenarioOutcome.Failed,
        stepResults,
        scenarioTimer.ElapsedMilliseconds,
        $"Log.Error during scenario: {errorMsg}",
        LogWatch.ErrorsSinceArmed,
        BuildAttachmentsWithScreenshot(ctx, screenshotPath),
        stateDumps);
  }

  // Only a real failure pauses, and only when a human is watching. An autorun has
  // nobody to resume it, so it would hang until the watchdog kills the process.
  private async Task MaybeBreakOnFailure(
      string featureName, string scenarioName, string? sourcePath, int scenarioIndex, StepResult stepResult) {
    if (stepResult.Status != StepStatus.Failed || !BreakOnFailureState.Enabled ||
        AutorunState.IsAutorunning || OnBreak == null) {
      return;
    }

    IsPausedForBreak = true;
    try {
      await OnBreak((featureName, scenarioName, sourcePath, scenarioIndex, stepResult));
    } finally {
      IsPausedForBreak = false;
    }
  }
}
