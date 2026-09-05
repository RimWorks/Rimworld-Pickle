using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Reports;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Run;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;

namespace RimWorks.Pickle.Web;

/// <summary>
/// Serializes the tree plus live run state for the dashboard. Built on the main thread
/// and published as a finished string, so the listener never walks a mutating dictionary.
/// </summary>
public static class RunnerSnapshot {
  private const string TrueLiteral = "true";
  private const string FalseLiteral = "false";

  public static string Build(
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures,
      IReadOnlyDictionary<(string SourcePath, int ScenarioIndex), ScenarioResult> results,
      RunSession? session,
      bool isRunning,
      Func<string, int, bool>? isSelected = null,
      RunnerWindow? runner = null) {
    StringBuilder json = new StringBuilder();
    json.Append('{');

    string status = DescribeStatus(isRunning, session);
    json.Append("\"status\":").Append(Json.Quote(status)).Append(',');
    json.Append("\"feature\":").Append(Json.Quote(session?.CurrentFeatureName ?? string.Empty)).Append(',');
    json.Append("\"scenario\":").Append(Json.Quote(session?.CurrentScenarioName ?? string.Empty)).Append(',');
    json.Append("\"step\":").Append(Json.Quote(session?.CurrentStepDisplay ?? string.Empty)).Append(',');
    json.Append("\"passed\":").Append(results.Values.Count(r => r.Outcome == ScenarioOutcome.Passed)).Append(',');
    json.Append("\"failed\":").Append(results.Values.Count(r => r.Outcome == ScenarioOutcome.Failed)).Append(',');
    json.Append("\"cancelRequested\":").Append(session?.CancelRequested == true ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"pauseRequested\":").Append(session?.PauseRequested == true ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"runScope\":").Append(Json.Quote(runner?.RunScope ?? "all")).Append(',');
    json.Append("\"runTotal\":").Append(runner?.RunScenarioCount ?? parsedFeatures.Sum(f => f.Plan.Scenarios.Count)).Append(',');
    json.Append("\"runCompleted\":").Append(runner?.CompletedScenarioCount ?? results.Count).Append(',');
    json.Append("\"fixtureBusy\":").Append(FixtureCommands.IsBusy ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"watch\":").Append(PickleRunMode.Current == PickleRunMode.Mode.Watch ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"breakOnFailure\":").Append(BreakOnFailureState.Enabled ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"includeWip\":").Append(IncludeWipState.Enabled ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"showRunPill\":").Append(RunPillState.Enabled ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"controllable\":").Append(isSelected != null ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"strings\":").Append(DashboardStrings.BuildJson()).Append(',');
    json.Append("\"search\":").Append(Json.Quote(runner?.SearchText ?? string.Empty)).Append(',');
    json.Append("\"modFilter\":").Append(Json.Quote(runner?.ModFilterSelection)).Append(',');
    json.Append("\"tagFilters\":").Append(Json.Array((runner?.ActiveTagFilters ?? []).Select(Json.Quote))).Append(',');
    json.Append("\"lastRunAt\":").Append(Json.Quote(runner?.LastRunAt?.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture))).Append(',');

    int scenarioIndex = 0;
    List<string> features = new List<string>();
    foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
      string sourcePath = plan.SourcePath ?? string.Empty;
      features.Add(BuildFeature(suite, plan, sourcePath, scenarioIndex, results, isSelected, session, runner));
      scenarioIndex += plan.Scenarios.Count;
    }

    json.Append("\"features\":").Append(Json.Array(features));
    json.Append('}');
    return json.ToString();
  }

  private static string BuildFeature(
      DiscoveredSuite suite,
      FeaturePlan plan,
      string sourcePath,
      int featureStartIndex,
      IReadOnlyDictionary<(string SourcePath, int ScenarioIndex), ScenarioResult> results,
      Func<string, int, bool>? isSelected,
      RunSession? session,
      RunnerWindow? runner) {
    List<string> scenarios = new List<string>();
    for (int i = 0; i < plan.Scenarios.Count; i++) {
      ScenarioPlan scenario = plan.Scenarios[i];
      int index = featureStartIndex + i;
      results.TryGetValue((sourcePath, index), out ScenarioResult? result);

      IReadOnlyList<StepResult>? liveSteps =
          session?.CurrentSourcePath == sourcePath && ReferenceEquals(session?.CurrentScenario, scenario)
              ? session.CurrentStepResults
              : null;

      scenarios.Add(BuildScenario(
          scenario, index, result, isSelected?.Invoke(sourcePath, index) ?? true, liveSteps,
          runner?.IsScenarioVisible(suite, plan, scenario) ?? true));
    }

    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"name\":").Append(Json.Quote(plan.Name)).Append(',');
    json.Append("\"mod\":").Append(Json.Quote(suite.ModName)).Append(',');
    json.Append("\"path\":").Append(Json.Quote(sourcePath)).Append(',');
    json.Append("\"tags\":").Append(Json.Array(plan.Tags.Select(Json.Quote))).Append(',');
    json.Append("\"scenarios\":").Append(Json.Array(scenarios));
    json.Append('}');
    return json.ToString();
  }

  private static string BuildScenario(
      ScenarioPlan plan,
      int index,
      ScenarioResult? result,
      bool selected,
      IReadOnlyList<StepResult>? liveSteps,
      bool visible) {
    if (liveSteps != null) {
      result = null;
    }

    // A finished scenario has real results. The one currently running has results for
    // the steps done so far, and the rest of its plan still reads Pending.
    IEnumerable<string> steps;
    if (result != null) {
      steps = result.Steps.Select(BuildStep);
    } else if (liveSteps != null) {
      steps = liveSteps.Select(BuildStep)
          .Concat(plan.Steps.Skip(liveSteps.Count).Select(BuildPlannedStep));
    } else {
      steps = plan.Steps.Select(BuildPlannedStep);
    }

    string outcome = result?.Outcome.ToString() ?? (liveSteps != null ? "Running" : "Pending");

    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"name\":").Append(Json.Quote(plan.Name)).Append(',');
    json.Append("\"index\":").Append(index).Append(',');
    json.Append("\"selected\":").Append(selected ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"visible\":").Append(visible ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"line\":").Append(plan.Line).Append(',');
    json.Append("\"tags\":").Append(Json.Array(plan.Tags.Select(Json.Quote))).Append(',');
    json.Append("\"outcome\":").Append(Json.Quote(outcome)).Append(',');
    json.Append("\"durationMs\":").Append(Json.Number(result?.DurationMs ?? 0)).Append(',');
    json.Append("\"attempts\":").Append(result?.Attempts ?? 1).Append(',');
    json.Append("\"tickCost\":").Append(BuildTickCost(result?.TickCost)).Append(',');
    json.Append("\"failedAttempts\":")
        .Append(Json.Array((result?.FailedAttempts ?? []).Select(BuildFailedAttempt))).Append(',');
    json.Append("\"failureMessage\":").Append(Json.Quote(result?.FailureMessage)).Append(',');
    json.Append("\"logTail\":").Append(Json.Array((result?.LogTail ?? []).Select(Json.Quote))).Append(',');
    json.Append("\"attachments\":").Append(Json.Array(EvidenceAttachments.Expand(result?.Attachments ?? []).Select(BuildAttachment))).Append(',');
    json.Append("\"stateDumps\":").Append(Json.Array((result?.StateDumps ?? []).Select(BuildStateDump))).Append(',');
    json.Append("\"steps\":").Append(Json.Array(steps));
    json.Append('}');
    return json.ToString();
  }

  private static string BuildTickCost((int Ticks, double MeanMs, double MaxMs)? cost) {
    if (!cost.HasValue) {
      return "null";
    }

    (int ticks, double meanMs, double maxMs) = cost.Value;
    return "{\"ticks\":" + ticks + ",\"meanMs\":" + Json.Number(meanMs) + ",\"maxMs\":" + Json.Number(maxMs) + "}";
  }

  private static string BuildFailedAttempt((int Attempt, string? Message) attempt) {
    return "{\"attempt\":" + attempt.Attempt + ",\"message\":" + Json.Quote(attempt.Message) + "}";
  }

  private static string BuildStateDump((string Source, string Content) dump) {
    return "{\"source\":" + Json.Quote(dump.Source) + ",\"content\":" + Json.Quote(dump.Content) + "}";
  }

  private static string BuildAttachment((string Name, string Content) attachment) {
    string content = attachment.Content;
    string root = ScreenshotCapture.ReportsDirectory() + Path.DirectorySeparatorChar;
    if (content.StartsWith(root, StringComparison.Ordinal)) {
      content = "/screenshots/" + string.Join("/", content.Substring(root.Length).Split(Path.DirectorySeparatorChar).Select(Uri.EscapeDataString));
    }

    return "{\"name\":" + Json.Quote(attachment.Name) + ",\"content\":" + Json.Quote(content) + "}";
  }

  private static string BuildStep(StepResult step) {
    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"keyword\":").Append(Json.Quote(step.Keyword.Trim())).Append(',');
    json.Append("\"text\":").Append(Json.Quote(step.Text)).Append(',');
    json.Append("\"status\":").Append(Json.Quote(step.Status.ToString())).Append(',');
    json.Append("\"durationMs\":").Append(Json.Number(step.DurationMs)).Append(',');
    json.Append("\"failureMessage\":").Append(Json.Quote(step.FailureMessage));
    json.Append('}');
    return json.ToString();
  }

  private static string BuildPlannedStep(StepPlan step) {
    StringBuilder json = new StringBuilder();
    json.Append('{');
    json.Append("\"keyword\":").Append(Json.Quote(step.Keyword.Trim())).Append(',');
    json.Append("\"text\":").Append(Json.Quote(step.Text)).Append(',');
    json.Append("\"status\":\"Pending\",");
    json.Append("\"durationMs\":0,");
    json.Append("\"failureMessage\":null");
    json.Append('}');
    return json.ToString();
  }

  private static string DescribeStatus(bool isRunning, RunSession? session) {
    if (!isRunning) {
      return "idle";
    }

    return session?.IsPaused == true ? "paused" : "running";
  }
}
