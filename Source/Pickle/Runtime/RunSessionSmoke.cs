using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gherkin;
using Gherkin.Ast;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using RimWorks.Pickle.Run;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>Runs a hand-written feature through a session and checks its outcomes and messages.</summary>
public static class RunSessionSmoke {
  /// <summary>Runs the smoke and logs whether it passed, catching any exception.</summary>
  /// <returns>A task that completes when the smoke finishes.</returns>
  public static async Task Run() {
    try {
      PickleDriver driver = PickleDriver.Instance;
      List<System.Reflection.Assembly> engineAssemblies = new() { typeof(SmokeSteps).Assembly };

      StepTable stepTable = StepScanner.PopulateStepTable(engineAssemblies);
      List<System.Type> stepsTypes = StepScanner.GetPickleStepsTypes(engineAssemblies);

      IReadOnlyList<DiscoveredSuite> suites = new List<DiscoveredSuite>();

      RunSession session = new RunSession(stepTable, driver, suites, stepsTypes);

      string gherkinSource = """
Feature: Run Session Smoke Test
  Scenario: Smoke step passes
    Given smoke step passes
    Then smoke step passes

  Scenario: Smoke step fails
    Given smoke step fails
    Then smoke step passes

  Scenario: Fluent smoke step
    Given fluent smoke step passes
""";

      Parser parser = new Parser();
      GherkinDocument doc = parser.Parse(new StringReader(gherkinSource));

      FeaturePlan plan = GherkinAdapter.Adapt(doc, null);

      List<ScenarioResult> results = await session.RunFeature(plan, "Pickle");

      if (results.Count != 3) {
        Log.ErrorTo("Pickle", "run session smoke failed: expected 3 scenarios, got {Count}", [results.Count]);
        return;
      }

      ScenarioResult first = results[0];
      ScenarioResult second = results[1];

      if (first.Outcome != ScenarioOutcome.Passed) {
        Log.ErrorTo("Pickle",
            "run session smoke failed: first scenario outcome is {Outcome}, expected Passed",
            [first.Outcome]);
        return;
      }

      if (second.Outcome != ScenarioOutcome.Failed) {
        Log.ErrorTo("Pickle",
            "run session smoke failed: second scenario outcome is {Outcome}, expected Failed",
            [second.Outcome]);
        return;
      }

      if (second.Steps.Count < 2 || second.Steps[1].Status != StepStatus.Skipped) {
        Log.ErrorTo("Pickle", "run session smoke failed: second scenario's later step was not skipped");
        return;
      }

      // Asserts the content, not just presence: a non-empty message passed while reflection
      // reported its own wrapper text instead of the assertion's.
      string failureMessage = second.FailureMessage ?? string.Empty;
      if (failureMessage.Length == 0 || !failureMessage.Contains("deliberate smoke failure")) {
        Log.ErrorTo("Pickle",
            "run session smoke failed: second scenario failure message should name "
            + "the assert, got: {FailureMessage}",
            [second.FailureMessage]);
        return;
      }

      ScenarioResult third = results[2];
      if (third.Outcome != ScenarioOutcome.Passed) {
        Log.ErrorTo("Pickle",
            "run session smoke failed: fluent scenario outcome is {Outcome}, "
            + "expected Passed ({FailureMessage})",
            [third.Outcome, third.FailureMessage]);
        return;
      }

      Log.InfoTo("Pickle", "run session smoke passed");
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, "run session smoke failed");
    }
  }
}
