using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Gherkin;
using Gherkin.Ast;
using Pickle.Core;
using Pickle.Core.Discovery;
using Pickle.Core.Model;
using Pickle.Core.Run;
using Pickle.Core.Steps;
using Pickle.Run;
using Verse;

namespace Pickle.Runtime;

public static class RunSessionSmoke {
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
        Log.Error($"pickle: run session smoke failed: expected 3 scenarios, got {results.Count}");
        return;
      }

      ScenarioResult first = results[0];
      ScenarioResult second = results[1];

      if (first.Outcome != ScenarioOutcome.Passed) {
        Log.Error($"pickle: run session smoke failed: first scenario outcome is {first.Outcome}, expected Passed");
        return;
      }

      if (second.Outcome != ScenarioOutcome.Failed) {
        Log.Error($"pickle: run session smoke failed: second scenario outcome is {second.Outcome}, expected Failed");
        return;
      }

      if (second.Steps.Count < 2 || second.Steps[1].Status != StepStatus.Skipped) {
        Log.Error("pickle: run session smoke failed: second scenario's later step was not skipped");
        return;
      }

      // Asserts the content, not just presence: a non-empty message passed while reflection
      // reported its own wrapper text instead of the assertion's.
      string failureMessage = second.FailureMessage ?? string.Empty;
      if (failureMessage.Length == 0 || !failureMessage.Contains("deliberate smoke failure")) {
        Log.Error(
            $"pickle: run session smoke failed: second scenario failure message should name the assert, got: {second.FailureMessage}");
        return;
      }

      ScenarioResult third = results[2];
      if (third.Outcome != ScenarioOutcome.Passed) {
        Log.Error($"pickle: run session smoke failed: fluent scenario outcome is {third.Outcome}, expected Passed ({third.FailureMessage})");
        return;
      }

      Log.Message("pickle: run session smoke passed");
    } catch (Exception ex) {
      Log.Error($"pickle: run session smoke failed: {ex}");
    }
  }
}
