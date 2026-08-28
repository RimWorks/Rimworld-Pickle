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
  private const float FixtureStepTimeoutSeconds = 180f;

  // Arbitrary fixed constant so a run with no -pickle-seed is still deterministic
  // run to run, matching what -pickle-seed=42 would produce.
  public const int DefaultSeed = 42;

  private List<StepResult> currentStepResults = new();

  private readonly StepTable stepTable;
  private readonly PickleDriver driver;
  private readonly IReadOnlyList<DiscoveredSuite> suites;
  private readonly List<Type> stepsTypes;
  private readonly int runSeed;

  private string? currentLoadedFixture;
  private string currentOwningMod = string.Empty;
  private TagSet currentScenarioTags = new TagSet(Array.Empty<string>());
  private readonly Dictionary<Type, object> scenarioInstanceCache = new();

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

      if (RunOutcomes.ShouldSkip(scenario.Tags, includeWip)) {
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

      ScenarioResult result = await RunScenario(scenario, plan.Name, owningModName, plan.SourcePath, scenarioIndex);
      results.Add(result);
      onScenarioCompleted?.Invoke(result);

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

  private async Task<ScenarioResult> RunScenario(ScenarioPlan scenario, string featureName, string owningModName, string? sourcePath, int scenarioIndex) {
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

    try {
      // Not Rand.PushState: Root.Update drains the stack whenever a step awaits past a
      // frame, so the matching PopState throws. Seed reseeds the same stream instead.
      Rand.Seed = scenarioSeed;
      LogWatch.Arm();

      await RunBeforeHooks(ctx, scenario.Tags);

      foreach (StepPlan step in scenario.Steps) {
        StepResult stepResult = await RunStep(ctx, step);
        stepResults.Add(stepResult);
        OnProgress?.Invoke();

        if (stepResult.Status == StepStatus.Failed || stepResult.Status == StepStatus.Undefined || stepResult.Status == StepStatus.Ambiguous) {
          if (stepResult.Status == StepStatus.Failed && BreakOnFailureState.Enabled && !AutorunState.IsAutorunning && OnBreak != null) {
            IsPausedForBreak = true;
            try {
              await OnBreak((featureName, scenario.Name, sourcePath, scenarioIndex, stepResult));
            } finally {
              IsPausedForBreak = false;
            }
          }

          foreach (StepPlan remainingStep in scenario.Steps.Skip(stepResults.Count)) {
            stepResults.Add(new StepResult(remainingStep.Keyword, remainingStep.Text, StepStatus.Skipped, 0));
          }
          break;
        }

        if (LogWatch.Armed && LogWatch.ErrorCount > 0 && !scenario.Tags.Contains("@allow-errors")) {
          IReadOnlyList<string> errorLog = LogWatch.ErrorsSinceArmed;
          string errorMsg = errorLog.Count > 0 ? errorLog[0] : "unknown error";
          scenarioTimer.Stop();

          int failingStepIndex = stepResults.Count + 1;
          (string? screenshotPath, List<(string Source, string Content)> stateDumps) =
              await CaptureEvidence(featureName, scenario.Name, failingStepIndex);

          foreach (StepPlan remainingStep in scenario.Steps.Skip(stepResults.Count)) {
            stepResults.Add(new StepResult(remainingStep.Keyword, remainingStep.Text, StepStatus.Skipped, 0));
          }

          await RunAfterHooks(ctx, scenario.Tags);

          List<(string Name, string Content)> attachmentsWithScreenshot = BuildAttachmentsWithScreenshot(ctx, screenshotPath);

          return new ScenarioResult(
              scenario.Name,
              featureName,
              scenario.Tags,
              ScenarioOutcome.Failed,
              stepResults,
              scenarioTimer.ElapsedMilliseconds,
              $"Log.Error during scenario: {errorMsg}",
              LogWatch.ErrorsSinceArmed,
              attachmentsWithScreenshot,
              stateDumps);
        }
      }

      await RunAfterHooks(ctx, scenario.Tags);

      scenarioTimer.Stop();

      ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(stepResults);
      string? failureMsg = null;

      if (outcome == ScenarioOutcome.Failed) {
        failureMsg = RunOutcomes.BuildUndefinedMessage(stepResults);
        if (string.IsNullOrEmpty(failureMsg)) {
          failureMsg = RunOutcomes.BuildAmbiguousMessage(stepResults);
        }
        if (string.IsNullOrEmpty(failureMsg)) {
          StepResult? failedStep = stepResults.FirstOrDefault(s => s.Status == StepStatus.Failed);
          failureMsg = failedStep?.FailureMessage ?? "Scenario failed";
        }

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

        float timeout = 5f;
        string? timeoutTag = currentScenarioTags.FirstOrDefault(t => t.StartsWith("@timeout:"));
        if (timeoutTag != null && float.TryParse(timeoutTag.Substring(9), out float parsedTimeout)) {
          timeout = parsedTimeout;
        }

        if (definition.TimeoutSeconds.HasValue) {
          timeout = definition.TimeoutSeconds.Value;
        }

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

    if (!method.IsStatic) {
      if (!scenarioInstanceCache.TryGetValue(declaringType, out instance)) {
        instance = Activator.CreateInstance(declaringType)!;
        scenarioInstanceCache[declaringType] = instance;
      }
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

  private Task InvokeDelegateBinding(PickleContext ctx, Delegate @delegate, IReadOnlyList<object?> args) {
    List<object?> allArgs = [ctx, .. args];
    object? result = @delegate.DynamicInvoke([.. allArgs]);

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
        if (beforeAttr != null) {
          if (beforeAttr.Tag == null || tags.Contains(beforeAttr.Tag)) {
            await InvokeHook(ctx, stepsType, method);
          }
        }
      }
    }
  }

  private async Task RunAfterHooks(PickleContext ctx, TagSet tags) {
    foreach (Type stepsType in stepsTypes) {
      MethodInfo[] methods = stepsType.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

      foreach (MethodInfo method in methods) {
        AfterScenarioAttribute? afterAttr = method.GetCustomAttribute<AfterScenarioAttribute>();
        if (afterAttr != null) {
          if (afterAttr.Tag == null || tags.Contains(afterAttr.Tag)) {
            await InvokeHook(ctx, stepsType, method);
          }
        }
      }
    }
  }

  private async Task InvokeHook(PickleContext ctx, Type stepsType, MethodInfo method) {
    object? instance = null;

    if (!method.IsStatic) {
      if (!scenarioInstanceCache.TryGetValue(stepsType, out instance)) {
        instance = Activator.CreateInstance(stepsType)!;
        scenarioInstanceCache[stepsType] = instance;
      }
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
    string pattern = "the save {string} is loaded";
    StepDefinition engineStep = new StepDefinition(
        pattern,
        StepKind.Given,
        "Pickle engine",
        new List<Type> { typeof(string) },
        new Func<PickleContext, string, Task>(LoadFixtureStep),
        FixtureStepTimeoutSeconds);

    stepTable.Add(engineStep);
  }

  private async Task LoadFixtureStep(PickleContext ctx, string fixtureName) {
    if (currentScenarioTags.Contains("@same-world") && currentLoadedFixture == fixtureName) {
      return;
    }

    FixtureResolution resolution = FixtureResolver.Resolve(fixtureName, currentOwningMod, suites);

    if (resolution.Error != null) {
      throw new InvalidOperationException(resolution.Error.Message);
    }

    AutorunState.SuppressingFixtureLoad = true;
    try {
      await FixtureLoader.LoadFixture(resolution.Fixture!.FullPath, driver, ctx.WaitScope);
    } finally {
      AutorunState.SuppressingFixtureLoad = false;
    }

    currentLoadedFixture = fixtureName;
    LogWatch.Arm();
  }

  private async Task<(string? screenshotPath, List<(string Source, string Content)> stateDumps)> CaptureEvidence(
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

  private List<(string Name, string Content)> BuildAttachmentsWithScreenshot(
      PickleContext ctx, string? screenshotPath) {
    List<(string Name, string Content)> attachmentsWithScreenshot = [.. ctx.Attachments];

    if (screenshotPath is { Length: > 0 }) {
      attachmentsWithScreenshot.Add(("screenshot", screenshotPath));
    }

    return attachmentsWithScreenshot;
  }
}
