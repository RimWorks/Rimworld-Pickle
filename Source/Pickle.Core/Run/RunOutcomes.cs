using System;
using System.Collections.Generic;
using System.Linq;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Steps;

namespace RimWorks.Pickle.Core.Run;

public static class RunOutcomes {
  private const string RequiresPrefix = "@requires:";

  // A step in any of these states stops the scenario; the rest are reported as skipped.
  public static bool EndsScenario(StepStatus status) {
    return status == StepStatus.Failed || status == StepStatus.Undefined || status == StepStatus.Ambiguous;
  }

  public static bool ShouldSkip(TagSet tags, bool includeWip = false) {
    return (!includeWip && tags.Contains("@wip")) || tags.Contains("@skip");
  }

  /// <summary>
  /// Names the first <c>@requires:</c> tag the caller reports as absent, or null when the
  /// scenario can run. The caller decides what counts as present, so this stays testable.
  /// </summary>
  public static string? MissingRequirement(TagSet tags, Func<string, bool> isPresent) {
    foreach (string tag in tags) {
      if (!tag.StartsWith(RequiresPrefix, StringComparison.OrdinalIgnoreCase)) {
        continue;
      }

      string wanted = tag.Substring(RequiresPrefix.Length).Trim();
      if (wanted.Length > 0 && !isPresent(wanted)) {
        return wanted;
      }
    }

    return null;
  }

  /// <summary>
  /// Passed, but not on the first try. One definition, because five writers and two
  /// dashboards all have to agree on what the badge means.
  /// </summary>
  public static bool IsFlaky(ScenarioResult result) {
    return result.Outcome == ScenarioOutcome.Passed && result.Attempts > 1;
  }

  /// <summary>Reads a whole number off a tag such as <c>@retry:2</c>, or null when absent.</summary>
  public static int? IntFromTag(TagSet tags, string prefix) {
    foreach (string tag in tags) {
      if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
          && int.TryParse(tag.Substring(prefix.Length), out int value)) {
        return value;
      }
    }

    return null;
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
