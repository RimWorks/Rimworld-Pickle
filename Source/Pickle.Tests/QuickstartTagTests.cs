using System.Collections.Generic;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class QuickstartTagTests {
  [Fact]
  public void NameIn_WithNoQuickstartTag_ReturnsNull() {
    Assert.Null(QuickstartTag.NameIn(new TagSet(["@wip", "@seed:42"])));
  }

  [Fact]
  public void NameIn_ReadsTheNameAfterThePrefix() {
    Assert.Equal("OnePlanetParity", QuickstartTag.NameIn(new TagSet(["@quickstart:OnePlanetParity"])));
  }

  [Fact]
  public void NameIn_IgnoresTagCaseButKeepsTheNameAsWritten() {
    Assert.Equal("OnePlanetParity", QuickstartTag.NameIn(new TagSet(["@QuickStart:OnePlanetParity"])));
  }

  [Fact]
  public void NameIn_WithNoNameAfterThePrefix_ReturnsNull() {
    Assert.Null(QuickstartTag.NameIn(new TagSet(["@quickstart:"])));
  }

  [Fact]
  public void Problems_WithQuickstartAlone_FindsNothing() {
    FeaturePlan plan = Feature(Scenario(["@quickstart:OnePlanetParity"], "the colony has 3 colonists"));
    Assert.Empty(QuickstartTag.Problems(plan));
  }

  [Fact]
  public void Problems_WithFixtureAlone_FindsNothing() {
    FeaturePlan plan = Feature(Scenario([], "the save \"one-planet\" is loaded"));
    Assert.Empty(QuickstartTag.Problems(plan));
  }

  [Fact]
  public void Problems_WithBoth_NamesTheScenarioAndBothSources() {
    FeaturePlan plan = Feature(Scenario(
        ["@quickstart:OnePlanetParity"],
        "the save \"one-planet\" is loaded"));

    string problem = Assert.Single(QuickstartTag.Problems(plan));
    Assert.Contains("one planet matches vanilla", problem);
    Assert.Contains("OnePlanetParity", problem);
    Assert.Contains("one-planet", problem);
  }

  [Fact]
  public void Problems_MatchesTheFixtureStepWhateverItsCase() {
    FeaturePlan plan = Feature(Scenario(
        ["@quickstart:OnePlanetParity"],
        "The Save \"one-planet\" Is Loaded"));

    Assert.Single(QuickstartTag.Problems(plan));
  }

  [Fact]
  public void Problems_WithAnUnnamedQuickstart_SaysTheNameIsMissing() {
    FeaturePlan plan = Feature(Scenario(["@quickstart:"], "the colony has 3 colonists"));

    string problem = Assert.Single(QuickstartTag.Problems(plan));
    Assert.Contains("no quickstart name", problem);
  }

  [Fact]
  public void Problems_ReportsEveryBadScenarioInTheFeature() {
    FeaturePlan plan = new FeaturePlan(
        "parity",
        new TagSet([]),
        [
            Scenario(["@quickstart:A"], "the save \"one\" is loaded"),
            Scenario(["@quickstart:B"], "the save \"two\" is loaded"),
            Scenario(["@quickstart:C"], "the colony has 3 colonists"),
        ],
        "parity.feature");

    Assert.Equal(2, QuickstartTag.Problems(plan).Count);
  }

  private static FeaturePlan Feature(ScenarioPlan scenario) {
    return new FeaturePlan("parity", new TagSet([]), [scenario], "parity.feature");
  }

  private static ScenarioPlan Scenario(IEnumerable<string> tags, params string[] stepTexts) {
    List<StepPlan> steps = [];
    for (int i = 0; i < stepTexts.Length; i++) {
      steps.Add(new StepPlan("Given ", stepTexts[i], [], null, i + 2));
    }

    return new ScenarioPlan("one planet matches vanilla", new TagSet(tags), steps, 1);
  }
}
