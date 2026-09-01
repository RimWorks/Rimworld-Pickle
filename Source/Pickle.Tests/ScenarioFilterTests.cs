using System.Collections.Generic;
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

  private static ScenarioPlan Scenario(string name, int line, params string[] tags) {
    return new ScenarioPlan(name, new TagSet(tags), new List<StepPlan>(), line);
  }
}
