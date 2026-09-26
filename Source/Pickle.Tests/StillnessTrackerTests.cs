using System;
using RimWorks.Pickle.Core.Ui;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class StillnessTrackerTests {
  [Fact]
  public void New_HasSeenNothingAndHasNotStoodStill() {
    StillnessTracker<int> tracker = new(3);

    Assert.False(tracker.EverSeen);
    Assert.False(tracker.HasStoodStill);
    Assert.Equal(0, tracker.StillFrames);
    Assert.Null(tracker.LastSeen);
  }

  [Fact]
  public void Observe_ThatManyIdenticalFrames_HasStoodStill() {
    StillnessTracker<int> tracker = new(3);

    tracker.Observe(7);
    tracker.Observe(7);
    Assert.False(tracker.HasStoodStill);

    tracker.Observe(7);
    Assert.True(tracker.HasStoodStill);
    Assert.Equal(3, tracker.StillFrames);
  }

  [Fact]
  public void Observe_AChangeRestartsTheRun() {
    StillnessTracker<int> tracker = new(3);

    tracker.Observe(7);
    tracker.Observe(7);
    tracker.Observe(8);

    Assert.Equal(1, tracker.StillFrames);
    Assert.Equal((int?)8, tracker.LastSeen);
    Assert.False(tracker.HasStoodStill);
  }

  [Fact]
  public void Observe_AMissingFrameRestartsTheRunAndKeepsTheLastValue() {
    StillnessTracker<int> tracker = new(3);

    tracker.Observe(7);
    tracker.Observe(7);
    tracker.Observe(null);
    tracker.Observe(7);

    Assert.Equal(1, tracker.StillFrames);
    Assert.Equal((int?)7, tracker.LastSeen);
    Assert.True(tracker.EverSeen);
  }

  [Fact]
  public void Observe_ReturningAfterAGapToTheSameValueDoesNotCountTheGap() {
    StillnessTracker<int> tracker = new(2);

    tracker.Observe(7);
    tracker.Observe(null);
    tracker.Observe(7);

    Assert.False(tracker.HasStoodStill);
  }

  [Fact]
  public void Observe_OnlyMissingFrames_NeverSeen() {
    StillnessTracker<int> tracker = new(2);

    tracker.Observe(null);
    tracker.Observe(null);

    Assert.False(tracker.EverSeen);
    Assert.Null(tracker.LastSeen);
  }

  [Fact]
  public void Observe_AfterStandingStill_StaysStillWhileUnchanged() {
    StillnessTracker<int> tracker = new(2);

    tracker.Observe(5);
    tracker.Observe(5);
    tracker.Observe(5);

    Assert.True(tracker.HasStoodStill);
    Assert.Equal(3, tracker.StillFrames);
  }

  [Fact]
  public void Constructor_LessThanOneFrame_Throws() {
    Assert.Throws<ArgumentOutOfRangeException>(() => new StillnessTracker<int>(0));
  }
}
