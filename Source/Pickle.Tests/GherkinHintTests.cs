using RimWorks.Pickle.Core.Model;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class GherkinHintTests {
  [Theory]
  [InlineData("#FeatureLine", "Feature:")]
  [InlineData("#StepLine", "Given")]
  [InlineData("#ExamplesLine", "Examples:")]
  [InlineData("#ScenarioLine", "Scenario:")]
  [InlineData("#DocStringSeparator", "doc string")]
  [InlineData("#TableRow", "table row")]
  [InlineData("#BackgroundLine", "Background:")]
  [InlineData("#RuleLine", "Rule:")]
  [InlineData("#TagLine", "tag")]
  [InlineData("#Language", "language:")]
  public void For_EveryExpectedTokenNamesWhatToWrite(string token, string mentions) {
    string? hint = GherkinHint.For([token], false);

    Assert.NotNull(hint);
    Assert.Contains(mentions, hint);
  }

  [Fact]
  public void For_TheMissingFeatureLineWinsOverEverythingElseExpected() {
    string? hint = GherkinHint.For(["#EOF", "#Language", "#TagLine", "#FeatureLine", "#Comment", "#Empty"], false);

    Assert.NotNull(hint);
    Assert.Contains("Feature:", hint);
  }

  [Fact]
  public void For_AStepIsMoreUsefulThanAScenarioWhenBothAreWanted() {
    string? hint = GherkinHint.For(["#ScenarioLine", "#StepLine"], false);

    Assert.NotNull(hint);
    Assert.Contains("Given", hint);
  }

  [Fact]
  public void For_EndOfFileSaysTheTextRanOut() {
    string? hint = GherkinHint.For(["#StepLine"], true);

    Assert.NotNull(hint);
    Assert.Contains("ends before", hint);
  }

  [Fact]
  public void For_OnlyEndOfFileExpectedMeansThereIsTrailingContent() {
    Assert.Equal("nothing may follow the end of the feature", GherkinHint.For(["#EOF"], false));
  }

  [Fact]
  public void For_NothingRecognisedGivesNoHintRatherThanAGuess() {
    Assert.Null(GherkinHint.For(["#Comment", "#Empty", "#Other"], false));
    Assert.Null(GherkinHint.For([], false));
    Assert.Null(GherkinHint.For(null, false));
  }

  [Fact]
  public void For_MatchesATokenNameWhateverItsCase() {
    Assert.NotNull(GherkinHint.For(["#featureline"], false));
  }

  [Fact]
  public void UnknownLanguage_PointsAtTheHeaderRatherThanRepeatingTheName() {
    Assert.Contains("# language:", GherkinHint.UnknownLanguage);
  }
}
