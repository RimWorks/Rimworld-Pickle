using System;
using System.Collections.Generic;
using System.Linq;
using Pickle.Core.Model;
using Pickle.Core.Steps;

namespace Pickle.Core.Run;

public static class RunOutcomes {
  public static bool ShouldSkip(TagSet tags, bool includeWip = false) {
    return (!includeWip && tags.Contains("@wip")) || tags.Contains("@skip");
  }

  public static ScenarioOutcome OutcomeFromSteps(IReadOnlyList<StepResult> steps) {
    foreach (StepResult step in steps) {
      if (step.Status == StepStatus.Failed || step.Status == StepStatus.Undefined || step.Status == StepStatus.Ambiguous) {
        return ScenarioOutcome.Failed;
      }
    }

    bool allSkipped = steps.Count > 0 && steps.All(s => s.Status == StepStatus.Skipped);
    if (allSkipped) {
      return ScenarioOutcome.Skipped;
    }

    return ScenarioOutcome.Passed;
  }

  public static string BuildUndefinedMessage(IReadOnlyList<StepResult> steps) {
    List<StepResult> undefinedSteps = [.. steps.Where(s => s.Status == StepStatus.Undefined)];
    if (undefinedSteps.Count == 0) {
      return string.Empty;
    }

    UndefinedStep? firstUndefined = null;
    foreach (StepResult step in steps) {
      if (step.Status == StepStatus.Undefined) {
        firstUndefined = new UndefinedStep(step.FailureMessage ?? string.Empty);
        break;
      }
    }

    if (firstUndefined?.Skeleton == null) {
      return "Undefined step encountered.";
    }

    return $"Undefined step:\n{firstUndefined.Skeleton}";
  }

  public static string BuildAmbiguousMessage(IReadOnlyList<StepResult> steps) {
    List<StepResult> ambiguousSteps = [.. steps.Where(s => s.Status == StepStatus.Ambiguous)];
    if (ambiguousSteps.Count == 0) {
      return string.Empty;
    }

    string? firstFailureMsg = ambiguousSteps[0].FailureMessage;
    if (string.IsNullOrEmpty(firstFailureMsg)) {
      return "Ambiguous step encountered.";
    }

    return $"Ambiguous step: {firstFailureMsg}";
  }
}
