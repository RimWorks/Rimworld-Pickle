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

namespace RimWorks.Pickle.Runtime;

public static class EvidenceSmoke {
  public static async Task Run() {
    try {
      PickleDriver driver = PickleDriver.Instance;
      List<System.Reflection.Assembly> engineAssemblies = new() { typeof(EvidenceSteps).Assembly };

      StepTable stepTable = StepScanner.PopulateStepTable(engineAssemblies);
      List<System.Type> stepsTypes = StepScanner.GetPickleStepsTypes(engineAssemblies);

      IReadOnlyList<DiscoveredSuite> suites = new List<DiscoveredSuite>();

      RunSession session = new RunSession(stepTable, driver, suites, stepsTypes);

      string gherkinSource = """
Feature: Evidence Capture Test
  Scenario: Evidence is captured on failure
    Given evidence step fails
""";

      Parser parser = new Parser();
      GherkinDocument doc = parser.Parse(new StringReader(gherkinSource));

      FeaturePlan plan = GherkinAdapter.Adapt(doc, null);

      List<ScenarioResult> results = await session.RunFeature(plan, "Pickle");

      if (results.Count != 1) {
        Log.Error($"pickle: evidence smoke failed: expected 1 scenario, got {results.Count}");
        return;
      }

      ScenarioResult result = results[0];

      if (result.Outcome != ScenarioOutcome.Failed) {
        Log.Error($"pickle: evidence smoke failed: scenario outcome is {result.Outcome}, expected Failed");
        return;
      }

      string failureMessage = result.FailureMessage ?? string.Empty;
      if (failureMessage.Length == 0 || !failureMessage.Contains("deliberate evidence failure")) {
        Log.Error($"pickle: evidence smoke failed: failure message missing or incorrect: {result.FailureMessage}");
        return;
      }

      if (result.LogTail == null || result.LogTail.Count == 0) {
        Log.Error("pickle: evidence smoke failed: LogTail is null or empty");
        return;
      }

      (string Name, string Content) noteAttachment = result.Attachments.FirstOrDefault(a => a.Name == "note");
      if (noteAttachment.Name == null) {
        Log.Error("pickle: evidence smoke failed: 'note' attachment not found");
        return;
      }

      if (noteAttachment.Content != "attached-value") {
        Log.Error($"pickle: evidence smoke failed: 'note' attachment has wrong value: {noteAttachment.Content}");
        return;
      }

      (string Source, string Content) evidenceDump = result.StateDumps.FirstOrDefault(d => d.Content == "evidence-dump-ok");
      if (evidenceDump.Content == null) {
        Log.Error("pickle: evidence smoke failed: state dump with 'evidence-dump-ok' not found");
        return;
      }

      (string Name, string Content) screenshotAttachment = result.Attachments.FirstOrDefault(a => a.Name == "screenshot");
      if (screenshotAttachment.Name == null) {
        Log.Error("pickle: evidence smoke failed: screenshot attachment not found");
        return;
      }

      string screenshotPath = screenshotAttachment.Content;
      if (!File.Exists(screenshotPath)) {
        Log.Error($"pickle: evidence smoke failed: screenshot file does not exist at {screenshotPath}");
        return;
      }

      FileInfo fileInfo = new FileInfo(screenshotPath);
      if (fileInfo.Length == 0) {
        Log.Error($"pickle: evidence smoke failed: screenshot file is empty (0 bytes)");
        return;
      }

      Log.Message("pickle: evidence smoke passed");
    } catch (Exception ex) {
      Log.Error($"pickle: evidence smoke failed: {ex}");
    }
  }
}
