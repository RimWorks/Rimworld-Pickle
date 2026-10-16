using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gherkin;
using Gherkin.Ast;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Model;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class GherkinWriterTests {
  [Fact]
  public void Write_PlainScenario_KeepsNameAndSteps() {
    FeaturePlan plan = Parse(@"
Feature: Test Feature
  Scenario: First Scenario
    Given a thing
    When it happens
    Then it worked
");

    string text = GherkinWriter.Write(plan, plan.Scenarios[0]);

    Assert.Contains("Feature: Test Feature", text);
    Assert.Contains("Scenario: First Scenario", text);
    FeaturePlan reparsed = Parse(text);
    ScenarioPlan only = Assert.Single(reparsed.Scenarios);
    Assert.Equal("First Scenario", only.Name);
    Assert.Equal(["a thing", "it happens", "it worked"], only.Steps.Select(s => s.Text));
  }

  [Fact]
  public void Write_SecondScenario_LeavesTheOthersOut() {
    FeaturePlan plan = Parse(@"
Feature: Test Feature
  Scenario: First
    Given one
  Scenario: Second
    Given two
");

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[1]));

    ScenarioPlan only = Assert.Single(reparsed.Scenarios);
    Assert.Equal("Second", only.Name);
    Assert.Equal("two", Assert.Single(only.Steps).Text);
  }

  [Fact]
  public void Write_BackgroundSteps_AreInlinedIntoTheScenario() {
    FeaturePlan plan = Parse(@"
Feature: Test Feature
  Background:
    Given the save Colony is loaded
  Scenario: Needs the save
    Then a pawn exists
");

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[0]));

    ScenarioPlan only = Assert.Single(reparsed.Scenarios);
    Assert.Equal(["the save Colony is loaded", "a pawn exists"], only.Steps.Select(s => s.Text));
  }

  [Fact]
  public void Write_OutlineRow_WritesTheSubstitutedStep() {
    FeaturePlan plan = Parse(@"
Feature: Test Feature
  Scenario Outline: Spawns <kind>
    Given a <kind> spawns
    Examples:
      | kind    |
      | muffalo |
      | boomrat |
");

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[1]));

    ScenarioPlan only = Assert.Single(reparsed.Scenarios);
    Assert.Equal("Spawns boomrat", only.Name);
    Assert.Equal("a boomrat spawns", Assert.Single(only.Steps).Text);
  }

  [Fact]
  public void Write_Tags_CarryFeatureAndScenarioTagsOver() {
    FeaturePlan plan = Parse(@"
@suite
Feature: Test Feature
  @slow
  Scenario: Tagged
    Given a thing
");

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[0]));

    ScenarioPlan only = Assert.Single(reparsed.Scenarios);
    Assert.True(only.Tags.Contains("@suite"));
    Assert.True(only.Tags.Contains("@slow"));
  }

  [Fact]
  public void Write_DataTable_SurvivesTheRoundTrip() {
    FeaturePlan plan = Parse(@"
Feature: Test Feature
  Scenario: Has a table
    Given these pawns
      | name  | age |
      | Randy | 40  |
");

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[0]));

    StepPlan step = Assert.Single(Assert.Single(reparsed.Scenarios).Steps);
    Assert.Equal(["name", "age"], step.Table[0]);
    Assert.Equal(["Randy", "40"], step.Table[1]);
  }

  [Fact]
  public void Write_TableCellWithAPipe_IsEscaped() {
    StepPlan step = new StepPlan("Given ", "these cells", [["a|b", @"c\d"]], null, 1);
    FeaturePlan plan = OneStep(step);

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[0]));

    StepPlan round = Assert.Single(Assert.Single(reparsed.Scenarios).Steps);
    Assert.Equal(["a|b", @"c\d"], round.Table[0]);
  }

  [Fact]
  public void Write_DocString_SurvivesTheRoundTrip() {
    FeaturePlan plan = Parse(@"
Feature: Test Feature
  Scenario: Has a doc string
    Given this text
      """"""
      first line
        indented
      """"""
");

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[0]));

    StepPlan step = Assert.Single(Assert.Single(reparsed.Scenarios).Steps);
    Assert.Equal("first line\n  indented", step.DocString);
  }

  [Fact]
  public void Write_DocStringHoldingTripleQuotes_UsesBackticksInstead() {
    StepPlan step = new StepPlan("Given ", "this text", [], "a \"\"\" inside", 1);
    FeaturePlan plan = OneStep(step);

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, plan.Scenarios[0]));

    StepPlan round = Assert.Single(Assert.Single(reparsed.Scenarios).Steps);
    Assert.Equal("a \"\"\" inside", round.DocString);
  }

  [Fact]
  public void Write_NamelessFeature_StillParses() {
    StepPlan step = new StepPlan("Given ", "a thing", [], null, 1);
    ScenarioPlan scenario = new ScenarioPlan("Unnamed", new TagSet([]), [step], 1);
    FeaturePlan plan = new FeaturePlan(string.Empty, new TagSet([]), [scenario], null);

    FeaturePlan reparsed = Parse(GherkinWriter.Write(plan, scenario));

    Assert.Equal("Unnamed", Assert.Single(reparsed.Scenarios).Name);
  }

  private static FeaturePlan OneStep(StepPlan step) {
    ScenarioPlan scenario = new ScenarioPlan("Built by hand", new TagSet([]), [step], 1);
    return new FeaturePlan("Test Feature", new TagSet([]), [scenario], null);
  }

  private static FeaturePlan Parse(string gherkin) {
    GherkinDocument doc = new Parser().Parse(new StringReader(gherkin));
    return GherkinAdapter.Adapt(doc, null);
  }
}
