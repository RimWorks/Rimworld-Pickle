using System;
using System.Collections.Generic;
using Pickle.Core.Model;
using Pickle.Core.Run;
using Xunit;

namespace Pickle.Tests;

public class RunOutcomesTests {
  [Fact]
  public void ShouldSkip_WithWipTag_ReturnsTrue() {
    TagSet tags = new TagSet(new[] { "@wip" });
    Assert.True(RunOutcomes.ShouldSkip(tags));
  }

  [Fact]
  public void ShouldSkip_WithSkipTag_ReturnsTrue() {
    TagSet tags = new TagSet(new[] { "@skip" });
    Assert.True(RunOutcomes.ShouldSkip(tags));
  }

  [Fact]
  public void ShouldSkip_WithoutSkipTags_ReturnsFalse() {
    TagSet tags = new TagSet(new[] { "@smoke", "@important" });
    Assert.False(RunOutcomes.ShouldSkip(tags));
  }

  [Fact]
  public void ShouldSkip_WithEmptyTags_ReturnsFalse() {
    TagSet tags = new TagSet(Array.Empty<string>());
    Assert.False(RunOutcomes.ShouldSkip(tags));
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
}
