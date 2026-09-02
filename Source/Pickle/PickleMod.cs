using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CucumberExpressions;
using Gherkin;
using Gherkin.Ast;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Patching;
using RimWorks.Pickle.Web;
using RimWorks.RimLogging;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle;

public class PickleMod : Mod {
  public PickleMod(ModContentPack content) : base(content) {
    // Before anything else logs: LogWatch is fed from this sink now, so an error raised
    // during startup is only recorded once the sink is registered.
    Logging.RegisterSink(new PickleLogSink());

    Log.Info("pickle: loaded");

    // Last point before RimWorld applies XML patches, which is the only chance to see
    // which mod patches which def.
    PatchBackends.ApplyEarliest();

    string featureText = @"Feature: Test
  Scenario: First
    Given step one
  
  Scenario: Second
    Given step two";

    StringReader reader = new StringReader(featureText);
    GherkinDocument gherkinDoc = new Parser().Parse(reader);

    int scenarioCount = 0;
    foreach (IHasLocation child in gherkinDoc.Feature.Children) {
      if (child is Scenario) {
        scenarioCount++;
      }
    }

    SimpleParameterTypeRegistry registry = new SimpleParameterTypeRegistry();
    CucumberExpression expression = new CucumberExpression("I have cukes", registry);

    Regex regex = expression.Regex;
    Match match = regex.Match("I have cukes");

    if (match.Success) {
      Log.Info("pickle: parsed {ScenarioCount} scenarios", [scenarioCount]);
    } else {
      Log.Error("pickle: expression match failed");
    }

    List<DiscoveredSuite> suites = SuiteScanner.DiscoverSuites();
    SuiteScanner.LogSuites(suites);

    PickleHttpServer.StartUnlessDisabled();
  }
}
