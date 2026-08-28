using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Gherkin;
using Gherkin.Ast;
using Pickle.Core;
using Pickle.Core.Model;
using Xunit;

namespace Pickle.Tests;

public class GherkinAdapterTests {
  [Fact]
  public void Adapt_EmptyDocument_ReturnsEmptyFeaturePlan() {
    string gherkin = string.Empty;
    Parser parser = new Parser();
    GherkinDocument doc = parser.Parse(new StringReader(gherkin));

    FeaturePlan plan = GherkinAdapter.Adapt(doc, null);

    Assert.Equal(string.Empty, plan.Name);
    Assert.Empty(plan.Scenarios);
  }

  [Fact]
  public void Adapt_PlainFeatureWithTwoScenarios_CreatesTwoScenarioPlans() {
    string gherkin = @"
Feature: Test Feature
  Scenario: First Scenario
    Given a step

  Scenario: Second Scenario
    When another step
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Equal("Test Feature", plan.Name);
    Assert.Equal(2, plan.Scenarios.Count);
    Assert.Equal("First Scenario", plan.Scenarios[0].Name);
    Assert.Equal("Second Scenario", plan.Scenarios[1].Name);
  }

  [Fact]
  public void Adapt_BackgroundPrependedToScenario_BackgroundStepsFirst() {
    string gherkin = @"
Feature: Test Feature
  Background:
    Given a background step

  Scenario: Test Scenario
    Given a scenario step
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    ScenarioPlan scenario = plan.Scenarios[0];
    Assert.Equal(2, scenario.Steps.Count);
    Assert.Equal("a background step", scenario.Steps[0].Text);
    Assert.Equal("a scenario step", scenario.Steps[1].Text);
  }

  [Fact]
  public void Adapt_ScenarioOutlineWithSingleExamples_ExpandsOneScenarioPerRow() {
    string gherkin = @"
Feature: Test Feature
  Scenario Outline: Test Outline
    Given a <item> step

    Examples:
      | item  |
      | apple |
      | banana |
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Equal(2, plan.Scenarios.Count);
    Assert.Equal("Test Outline", plan.Scenarios[0].Name);
    Assert.Equal("a apple step", plan.Scenarios[0].Steps[0].Text);
    Assert.Equal("a banana step", plan.Scenarios[1].Steps[0].Text);
  }

  [Fact]
  public void Adapt_OutlineWithTableSubstitution_ReplacesPlaceholdersInTableCells() {
    string gherkin = @"
Feature: Test Feature
  Scenario Outline: Test with Table
    Given a step with table:
      | name  | value   |
      | <key> | <value> |

    Examples:
      | key | value |
      | a   | 1     |
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    StepPlan step = plan.Scenarios[0].Steps[0];
    Assert.NotEmpty(step.Table);
    Assert.Equal("a", step.Table[1][0]);
    Assert.Equal("1", step.Table[1][1]);
  }

  [Fact]
  public void Adapt_OutlineWithDocStringSubstitution_ReplacesPlaceholdersInDocString() {
    string gherkin = @"
Feature: Test Feature
  Scenario Outline: Test with DocString
    Given a step with text:
      """"""
      Value is <value>
      """"""

    Examples:
      | value |
      | 42    |
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    StepPlan step = plan.Scenarios[0].Steps[0];
    Assert.NotNull(step.DocString);
    Assert.Contains("Value is 42", step.DocString);
  }

  [Fact]
  public void Adapt_OutlineWithNameSubstitution_ReplacesPlaceholdersInName() {
    string gherkin = @"
Feature: Test Feature
  Scenario Outline: Test <item>
    Given a step

    Examples:
      | item  |
      | apple |
      | banana |
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Equal(2, plan.Scenarios.Count);
    Assert.Equal("Test apple", plan.Scenarios[0].Name);
    Assert.Equal("Test banana", plan.Scenarios[1].Name);
  }

  [Fact]
  public void Adapt_FeatureTagsInheritToScenarios() {
    string gherkin = @"
@feature_tag
Feature: Test Feature
  Scenario: Test Scenario
    Given a step
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    Assert.Contains("@feature_tag", plan.Scenarios[0].Tags);
  }

  [Fact]
  public void Adapt_RuleBlockFlattens_ScenariosBecomePlainScenarios() {
    string gherkin = @"
Feature: Test Feature
  Rule: A rule
    Scenario: Scenario in rule
      Given a step
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    Assert.Equal("Scenario in rule", plan.Scenarios[0].Name);
  }

  [Fact]
  public void Adapt_RuleTagsInheritToScenarios() {
    string gherkin = @"
@feature_tag
Feature: Test Feature
  @rule_tag
  Rule: A rule
    Scenario: Scenario in rule
      Given a step
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    Assert.Contains("@feature_tag", plan.Scenarios[0].Tags);
    Assert.Contains("@rule_tag", plan.Scenarios[0].Tags);
  }

  [Fact]
  public void Adapt_RuleBackgroundAppliesToRuleScenariosOnly() {
    string gherkin = @"
Feature: Test Feature
  Background:
    Given a feature background step

  Rule: A rule
    Background:
      Given a rule background step

    Scenario: Scenario in rule
      Given a scenario step
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    ScenarioPlan scenario = plan.Scenarios[0];
    Assert.Equal(3, scenario.Steps.Count);
    Assert.Equal("a feature background step", scenario.Steps[0].Text);
    Assert.Equal("a rule background step", scenario.Steps[1].Text);
    Assert.Equal("a scenario step", scenario.Steps[2].Text);
  }

  [Fact]
  public void Adapt_OutlineAndExamplesTagsMerge() {
    string gherkin = @"
@feature_tag
Feature: Test Feature
  @outline_tag
  Scenario Outline: Test Outline
    Given a step

    @examples_tag
    Examples:
      | unused |
      | value  |
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    TagSet tags = plan.Scenarios[0].Tags;
    Assert.Contains("@feature_tag", tags);
    Assert.Contains("@outline_tag", tags);
    Assert.Contains("@examples_tag", tags);
  }

  [Fact]
  public void Adapt_MultipleExampleTables_ExpandsAllRows() {
    string gherkin = @"
Feature: Test Feature
  Scenario Outline: Test Outline
    Given a <item> step

    Examples:
      | item  |
      | apple |

    Examples:
      | item   |
      | banana |
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Equal(2, plan.Scenarios.Count);
    Assert.Equal("a apple step", plan.Scenarios[0].Steps[0].Text);
    Assert.Equal("a banana step", plan.Scenarios[1].Steps[0].Text);
  }

  [Fact]
  public void Adapt_TableIncludesHeaderRow() {
    string gherkin = @"
Feature: Test Feature
  Scenario: Test Scenario
    Given a step with table:
      | header1 | header2 |
      | cell1   | cell2   |
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    StepPlan step = plan.Scenarios[0].Steps[0];
    Assert.Equal(2, step.Table.Count);
    Assert.Equal("header1", step.Table[0][0]);
    Assert.Equal("cell1", step.Table[1][0]);
  }

  [Fact]
  public void Adapt_StepPreservesKeywordAndLine() {
    string gherkin = @"
Feature: Test Feature
  Scenario: Test Scenario
    Given a given step
    When a when step
    Then a then step
";

    FeaturePlan plan = ParseFeature(gherkin);

    Assert.Single(plan.Scenarios);
    List<StepPlan> steps = [.. plan.Scenarios[0].Steps];
    Assert.Equal("Given ", steps[0].Keyword);
    Assert.Equal("When ", steps[1].Keyword);
    Assert.Equal("Then ", steps[2].Keyword);
  }

  private static FeaturePlan ParseFeature(string gherkinText) {
    Parser parser = new Parser();
    GherkinDocument doc = parser.Parse(new StringReader(gherkinText));
    return GherkinAdapter.Adapt(doc, null);
  }
}
