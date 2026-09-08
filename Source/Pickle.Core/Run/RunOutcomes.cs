using System;
using System.Collections.Generic;
using System.Linq;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Steps;

namespace RimWorks.Pickle.Core.Run;

/// <summary>The rules that turn step results and tags into a scenario outcome.</summary>
public static class RunOutcomes {
  private const string RequiresPrefix = "@requires:";

  /// <summary>Whether a step in this state stops the scenario.</summary>
  /// <param name="status">The status the step finished in.</param>
  /// <returns><c>true</c> when the remaining steps should be skipped.</returns>
  // A step in any of these states stops the scenario; the rest are reported as skipped.
  public static bool EndsScenario(StepStatus status) {
    return status == StepStatus.Failed || status == StepStatus.Undefined || status == StepStatus.Ambiguous;
  }

  /// <summary>Whether the scenario's tags mark it as skipped before it runs.</summary>
  /// <param name="tags">The scenario's tags.</param>
  /// <param name="includeWip">Whether a <c>@wip</c> scenario should run anyway.</param>
  /// <returns><c>true</c> when the scenario should not run.</returns>
  public static bool ShouldSkip(TagSet tags, bool includeWip = false) {
    return (!includeWip && tags.Contains("@wip")) || tags.Contains("@skip");
  }

  /// <summary>
  /// Names the first <c>@requires:</c> tag the caller reports as absent, or null when the
  /// scenario can run. The caller decides what counts as present, so this stays testable.
  /// </summary>
  /// <param name="tags">The scenario's tags.</param>
  /// <param name="isPresent">Answers whether a named requirement is loaded.</param>
  /// <returns>The missing requirement's name, or <c>null</c> when nothing is missing.</returns>
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
  /// <param name="result">The finished scenario.</param>
  /// <returns><c>true</c> when the scenario passed on a retry.</returns>
  public static bool IsFlaky(ScenarioResult result) {
    return result.Outcome == ScenarioOutcome.Passed && result.Attempts > 1;
  }

  /// <summary>Reads a whole number off a tag such as <c>@retry:2</c>, or null when absent.</summary>
  /// <param name="tags">The scenario's tags.</param>
  /// <param name="prefix">The tag prefix to read the number off, such as <c>@retry:</c>.</param>
  /// <returns>The number, or <c>null</c> when no tag carries the prefix.</returns>
  public static int? IntFromTag(TagSet tags, string prefix) {
    foreach (string tag in tags) {
      if (tag.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)
          && int.TryParse(tag.Substring(prefix.Length), out int value)) {
        return value;
      }
    }

    return null;
  }

  /// <summary>The outcome a scenario's step results add up to.</summary>
  /// <param name="steps">Every step result, in the order they ran.</param>
  /// <returns>Failed when any step failed, skipped when they all skipped, passed otherwise.</returns>
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

  /// <summary>The failure message for the first undefined step, carrying its skeleton.</summary>
  /// <param name="steps">Every step result, in the order they ran.</param>
  /// <returns>The message, or an empty string when no step was undefined.</returns>
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

  /// <summary>The failure message for the first step that matched more than one definition.</summary>
  /// <param name="steps">Every step result, in the order they ran.</param>
  /// <returns>The message, or an empty string when no step was ambiguous.</returns>
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
