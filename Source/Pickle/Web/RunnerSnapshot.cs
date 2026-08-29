using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Pickle.Core.Discovery;
using Pickle.Core.Model;
using Pickle.Core.Run;
using Pickle.Run;
using Pickle.Runtime;

namespace Pickle.Web;

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
      Func<string, int, bool>? isSelected = null) {
    StringBuilder json = new StringBuilder();
    json.Append('{');

    string status = DescribeStatus(isRunning, session);
    json.Append("\"status\":").Append(Json.Quote(status)).Append(',');
    json.Append("\"feature\":").Append(Json.Quote(session?.CurrentFeatureName ?? string.Empty)).Append(',');
    json.Append("\"scenario\":").Append(Json.Quote(session?.CurrentScenarioName ?? string.Empty)).Append(',');
    json.Append("\"step\":").Append(Json.Quote(session?.CurrentStepDisplay ?? string.Empty)).Append(',');
    json.Append("\"passed\":").Append(session?.PassedCount ?? 0).Append(',');
    json.Append("\"failed\":").Append(session?.FailedCount ?? 0).Append(',');
    json.Append("\"cancelRequested\":").Append(session?.CancelRequested == true ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"watch\":").Append(PickleRunMode.Current == PickleRunMode.Mode.Watch ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"breakOnFailure\":").Append(BreakOnFailureState.Enabled ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"controllable\":").Append(isSelected != null ? TrueLiteral : FalseLiteral).Append(',');
    json.Append("\"strings\":").Append(DashboardStrings.BuildJson()).Append(',');

    int scenarioIndex = 0;
    List<string> features = new List<string>();
    foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
      string sourcePath = plan.SourcePath ?? string.Empty;
      features.Add(BuildFeature(suite, plan, sourcePath, scenarioIndex, results, isSelected, session));
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
      RunSession? session) {
    bool featureRunning = session != null && session.CurrentFeatureName == plan.Name;

    List<string> scenarios = new List<string>();
    for (int i = 0; i < plan.Scenarios.Count; i++) {
      ScenarioPlan scenario = plan.Scenarios[i];
      int index = featureStartIndex + i;
      results.TryGetValue((sourcePath, index), out ScenarioResult? result);

      IReadOnlyList<StepResult>? liveSteps =
          featureRunning && session!.CurrentScenarioName == scenario.Name
              ? session.CurrentStepResults
              : null;

      scenarios.Add(BuildScenario(
          scenario, index, result, isSelected?.Invoke(sourcePath, index) ?? true, liveSteps));
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
      IReadOnlyList<StepResult>? liveSteps) {
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
    json.Append("\"line\":").Append(plan.Line).Append(',');
    json.Append("\"tags\":").Append(Json.Array(plan.Tags.Select(Json.Quote))).Append(',');
    json.Append("\"outcome\":").Append(Json.Quote(outcome)).Append(',');
    json.Append("\"durationMs\":").Append(Json.Number(result?.DurationMs ?? 0)).Append(',');
    json.Append("\"failureMessage\":").Append(Json.Quote(result?.FailureMessage)).Append(',');
    json.Append("\"logTail\":").Append(Json.Array((result?.LogTail ?? []).Select(Json.Quote))).Append(',');
    json.Append("\"attachments\":").Append(Json.Array((result?.Attachments ?? []).Select(BuildAttachment))).Append(',');
    json.Append("\"stateDumps\":").Append(Json.Array((result?.StateDumps ?? []).Select(BuildStateDump))).Append(',');
    json.Append("\"steps\":").Append(Json.Array(steps));
    json.Append('}');
    return json.ToString();
  }

  private static string BuildStateDump((string Source, string Content) dump) {
    return "{\"source\":" + Json.Quote(dump.Source) + ",\"content\":" + Json.Quote(dump.Content) + "}";
  }

  private static string BuildAttachment((string Name, string Content) attachment) {
    return "{\"name\":" + Json.Quote(attachment.Name) + ",\"content\":" + Json.Quote(attachment.Content) + "}";
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

    return session?.IsPausedForBreak == true ? "paused" : "running";
  }
}
