using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using CucumberExpressions;
using Gherkin;
using Gherkin.Ast;
using Pickle.Core.Discovery;
using Pickle.Web;
using Verse;

namespace Pickle;

public class PickleMod : Mod {
  public PickleMod(ModContentPack content) : base(content) {
    Log.Message("pickle: loaded");

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
      Log.Message($"pickle: parsed {scenarioCount} scenarios");
    } else {
      Log.Error("pickle: expression match failed");
    }

    List<DiscoveredSuite> suites = SuiteScanner.DiscoverSuites();
    SuiteScanner.LogSuites(suites);

    PickleHttpServer.StartIfRequested();
  }
}
