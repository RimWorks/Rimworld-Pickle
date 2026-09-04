using System;
using System.Collections.Generic;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class ScenarioFilterTests {
  private const string Path = "/mods/MyMod/Pickle/Features/pawn-steps.feature";

  [Fact]
  public void Tag_term_matches_a_tagged_scenario() {
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12, "@film"), "@film"));
    Assert.False(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "@film"));
  }

  [Fact]
  public void Mod_name_term_matches_every_scenario_in_that_mod() {
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "MyMod"));
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "mymod"));
    Assert.False(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "OtherMod"));
  }

  [Fact]
  public void File_name_term_matches_the_whole_feature() {
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "pawn-steps.feature"));
    Assert.False(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "map-steps.feature"));
  }

  [Fact]
  public void Name_term_matches_a_scenario_by_substring() {
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("a pawn walks home", 12), "::walks"));
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("a pawn walks home", 12), "::WALKS"));
    Assert.False(ScenarioFilter.Matches("MyMod", Path, Scenario("a pawn walks home", 12), "::sprints"));
  }

  [Fact]
  public void Name_term_can_be_scoped_to_one_feature() {
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "pawn-steps.feature::walks"));
    Assert.False(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "map-steps.feature::walks"));
  }

  [Fact]
  public void Line_term_matches_one_scenario() {
    Assert.True(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "pawn-steps.feature:12"));
    Assert.False(ScenarioFilter.Matches("MyMod", Path, Scenario("walks", 12), "pawn-steps.feature:13"));
  }

  [Fact]
  public void A_windows_path_is_not_read_as_a_line_number() {
    const string windows = @"C:\mods\MyMod\Pickle\Features\pawn-steps.feature";
    Assert.True(ScenarioFilter.Matches("MyMod", windows, Scenario("walks", 12), "pawn-steps.feature"));
  }

  [Fact]
  public void Terms_split_on_commas_and_drop_blanks() {
    Assert.Equal(["@film", "MyMod"], ScenarioFilter.SplitTerms(" @film , MyMod , "));
    Assert.Empty(ScenarioFilter.SplitTerms(null));
    Assert.Empty(ScenarioFilter.SplitTerms(string.Empty));
  }

  [Fact]
  public void An_unmatched_filter_throws_and_lists_what_is_available() {
    InvalidOperationException ex = Assert.Throws<InvalidOperationException>(
        () => ScenarioFilter.FilterFeatures(Features(), "rings"));

    Assert.Contains("filter 'rings' matched no scenarios", ex.Message, StringComparison.Ordinal);
    Assert.Contains("2 features in MyMod: pawn-steps.feature, map-steps.feature", ex.Message, StringComparison.Ordinal);
    Assert.Contains("terms are @tag", ex.Message, StringComparison.Ordinal);
  }

  [Fact]
  public void An_empty_filter_keeps_every_feature() {
    Assert.Equal(2, ScenarioFilter.FilterFeatures(Features(), null).Count);
    Assert.Equal(2, ScenarioFilter.FilterFeatures(Features(), string.Empty).Count);
  }

  [Fact]
  public void A_matching_filter_keeps_only_the_scenarios_it_picks() {
    List<(DiscoveredSuite Suite, FeaturePlan Plan)> kept =
        ScenarioFilter.FilterFeatures(Features(), "pawn-steps.feature::walks");

    (DiscoveredSuite _, FeaturePlan plan) = Assert.Single(kept);
    Assert.Equal("walks", Assert.Single(plan.Scenarios).Name);
  }

  private static List<(DiscoveredSuite Suite, FeaturePlan Plan)> Features() {
    DiscoveredSuite suite = new DiscoveredSuite("MyMod", "fx", "fx", [], [], [], []);
    return [
      (suite, new FeaturePlan("pawn", new TagSet([]), [Scenario("walks", 12), Scenario("sleeps", 20)], Path)),
      (suite, new FeaturePlan("map", new TagSet([]), [Scenario("grows", 8)], "/mods/MyMod/Pickle/Features/map-steps.feature")),
    ];
  }

  private static ScenarioPlan Scenario(string name, int line, params string[] tags) {
    return new ScenarioPlan(name, new TagSet(tags), new List<StepPlan>(), line);
  }
}
