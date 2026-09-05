using System;
using System.Collections.Generic;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class RunOutcomesTests {
  private static readonly string[] WipTags = ["@wip"];
  private static readonly string[] SkipTags = ["@skip"];
  private static readonly string[] PlainTags = ["@smoke", "@important"];

  [Fact]
  public void ShouldSkip_WithWipTag_ReturnsTrue() {
    TagSet tags = new TagSet(WipTags);
    Assert.True(RunOutcomes.ShouldSkip(tags));
  }

  [Fact]
  public void ShouldSkip_WithSkipTag_ReturnsTrue() {
    TagSet tags = new TagSet(SkipTags);
    Assert.True(RunOutcomes.ShouldSkip(tags));
  }

  [Fact]
  public void ShouldSkip_WithoutSkipTags_ReturnsFalse() {
    TagSet tags = new TagSet(PlainTags);
    Assert.False(RunOutcomes.ShouldSkip(tags));
  }

  [Fact]
  public void ShouldSkip_WithEmptyTags_ReturnsFalse() {
    TagSet tags = new TagSet(Array.Empty<string>());
    Assert.False(RunOutcomes.ShouldSkip(tags));
  }

  [Fact]
  public void MissingRequirement_WhenPresent_ReturnsNull() {
    TagSet tags = new TagSet(["@requires:Ideology"]);
    Assert.Null(RunOutcomes.MissingRequirement(tags, _ => true));
  }

  [Fact]
  public void MissingRequirement_WhenAbsent_NamesIt() {
    TagSet tags = new TagSet(["@slow", "@requires:Anomaly"]);
    Assert.Equal("Anomaly", RunOutcomes.MissingRequirement(tags, _ => false));
  }

  [Fact]
  public void MissingRequirement_ChecksEveryRequirement() {
    TagSet tags = new TagSet(["@requires:Royalty", "@requires:Anomaly"]);
    Assert.Equal("Anomaly", RunOutcomes.MissingRequirement(tags, name => name == "Royalty"));
  }

  [Fact]
  public void MissingRequirement_IgnoresTagCase() {
    TagSet tags = new TagSet(["@Requires:Anomaly"]);
    Assert.Equal("Anomaly", RunOutcomes.MissingRequirement(tags, _ => false));
  }

  [Fact]
  public void MissingRequirement_WithoutRequiresTags_ReturnsNull() {
    TagSet tags = new TagSet(PlainTags);
    Assert.Null(RunOutcomes.MissingRequirement(tags, _ => false));
  }

  [Fact]
  public void MissingRequirement_WithEmptyName_IsIgnored() {
    TagSet tags = new TagSet(["@requires:"]);
    Assert.Null(RunOutcomes.MissingRequirement(tags, _ => false));
  }

  [Fact]
  public void OutcomeFromSteps_AllPassed_ReturnsPassed() {
    List<StepResult> steps = new()
    {
            new StepResult("Given", "step 1", StepStatus.Passed, 100),
            new StepResult("When", "step 2", StepStatus.Passed, 200),
    };

    ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(steps);
    Assert.Equal(ScenarioOutcome.Passed, outcome);
  }

  [Fact]
  public void OutcomeFromSteps_OneFailed_ReturnsFailed() {
    List<StepResult> steps = new()
    {
            new StepResult("Given", "step 1", StepStatus.Passed, 100),
            new StepResult("When", "step 2", StepStatus.Failed, 200, "assertion failed"),
    };

    ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(steps);
    Assert.Equal(ScenarioOutcome.Failed, outcome);
  }

  [Fact]
  public void OutcomeFromSteps_OneUndefined_ReturnsFailed() {
    List<StepResult> steps = new()
    {
            new StepResult("Given", "step 1", StepStatus.Passed, 100),
            new StepResult("When", "step 2", StepStatus.Undefined, 0, "skeleton"),
    };

    ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(steps);
    Assert.Equal(ScenarioOutcome.Failed, outcome);
  }

  [Fact]
  public void OutcomeFromSteps_OneAmbiguous_ReturnsFailed() {
    List<StepResult> steps = new()
    {
            new StepResult("Given", "step 1", StepStatus.Passed, 100),
            new StepResult("When", "step 2", StepStatus.Ambiguous, 0, "multiple matches"),
    };

    ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(steps);
    Assert.Equal(ScenarioOutcome.Failed, outcome);
  }

  [Fact]
  public void OutcomeFromSteps_AllSkipped_ReturnsSkipped() {
    List<StepResult> steps = new()
    {
            new StepResult("Given", "step 1", StepStatus.Skipped, 0),
            new StepResult("When", "step 2", StepStatus.Skipped, 0),
    };

    ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(steps);
    Assert.Equal(ScenarioOutcome.Skipped, outcome);
  }

  [Fact]
  public void OutcomeFromSteps_MixedPassedAndSkipped_ReturnsPassed() {
    List<StepResult> steps = new()
    {
            new StepResult("Given", "step 1", StepStatus.Passed, 100),
            new StepResult("When", "step 2", StepStatus.Skipped, 0),
    };

    ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(steps);
    Assert.Equal(ScenarioOutcome.Passed, outcome);
  }

  [Fact]
  public void OutcomeFromSteps_EmptyList_ReturnsPassed() {
    List<StepResult> steps = new();
    ScenarioOutcome outcome = RunOutcomes.OutcomeFromSteps(steps);
    Assert.Equal(ScenarioOutcome.Passed, outcome);
  }

  [Fact]
  public void IsFlaky_PassedAfterARetry() {
    Assert.True(RunOutcomes.IsFlaky(Result(ScenarioOutcome.Passed, attempts: 2)));
  }

  [Fact]
  public void IsFlaky_PassedFirstTimeIsNotFlaky() {
    Assert.False(RunOutcomes.IsFlaky(Result(ScenarioOutcome.Passed, attempts: 1)));
  }

  [Fact]
  public void IsFlaky_FailedIsNeverFlakyHoweverManyAttempts() {
    Assert.False(RunOutcomes.IsFlaky(Result(ScenarioOutcome.Failed, attempts: 4)));
  }

  [Fact]
  public void IsFlaky_SkippedIsNotFlaky() {
    Assert.False(RunOutcomes.IsFlaky(Result(ScenarioOutcome.Skipped, attempts: 2)));
  }

  [Fact]
  public void IntFromTag_ReadsTheNumber() {
    Assert.Equal(2, RunOutcomes.IntFromTag(new TagSet(["@wip", "@retry:2"]), "@retry:"));
  }

  [Fact]
  public void IntFromTag_MissingTagIsNull() {
    Assert.Null(RunOutcomes.IntFromTag(new TagSet(["@wip"]), "@retry:"));
  }

  [Fact]
  public void IntFromTag_NonNumericTagIsNull() {
    Assert.Null(RunOutcomes.IntFromTag(new TagSet(["@retry:lots"]), "@retry:"));
  }

  [Fact]
  public void IntFromTag_DoesNotConfuseSeedWithRetry() {
    TagSet tags = new TagSet(["@seed:99", "@retry:1"]);

    Assert.Equal(99, RunOutcomes.IntFromTag(tags, "@seed:"));
    Assert.Equal(1, RunOutcomes.IntFromTag(tags, "@retry:"));
  }

  private static ScenarioResult Result(ScenarioOutcome outcome, int attempts) {
    return new ScenarioResult("s", "f", new TagSet([]), outcome, [], 0) { Attempts = attempts };
  }
}
