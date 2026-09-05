using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

// The sampler is static, like LogWatch, because the step that feeds it and the session
// that reads it share no object. Every test resets first.
public class TickCostSamplerTests {
  [Fact]
  public void Window_EmptySamplerReportsNothing() {
    TickCostSampler.Reset();

    TickCostWindow window = TickCostSampler.Window(100);

    Assert.Equal(0, window.Count);
    Assert.Equal(0, TickCostSampler.Count);
  }

  [Fact]
  public void Window_MeanOverKnownSamples() {
    TickCostSampler.Reset();
    RecordMs(2, 4, 6);

    TickCostWindow window = TickCostSampler.Window(3);

    Assert.Equal(3, window.Count);
    Assert.Equal(4d, window.MeanMs, 3);
  }

  [Fact]
  public void Window_MaxAndItsDistanceFromTheNewest() {
    TickCostSampler.Reset();
    RecordMs(1, 9, 1, 1);

    TickCostWindow window = TickCostSampler.Window(4);

    Assert.Equal(9d, window.MaxMs, 3);

    // Newest is zero, so the 9 sits two back.
    Assert.Equal(2, window.MaxIndexFromEnd);
  }

  [Fact]
  public void Window_AsksForMoreThanRecorded_ReportsWhatItHas() {
    TickCostSampler.Reset();
    RecordMs(5, 5);

    TickCostWindow window = TickCostSampler.Window(2000);

    Assert.Equal(2, window.Count);
    Assert.Equal(5d, window.MeanMs, 3);
  }

  [Fact]
  public void Window_RingWraps_KeepsTheNewestCapacitySamples() {
    TickCostSampler.Reset();

    // One cheap tick, then a full ring of expensive ones: the cheap one falls out.
    RecordMs(100);
    for (int i = 0; i < TickCostSampler.Capacity; i++) {
      RecordMs(1);
    }

    TickCostWindow window = TickCostSampler.Window(TickCostSampler.Capacity);

    Assert.Equal(TickCostSampler.Capacity, window.Count);
    Assert.Equal(1d, window.MaxMs, 3);
    Assert.Equal(TickCostSampler.Capacity + 1, TickCostSampler.Count);
  }

  [Fact]
  public void Window_ZeroOrNegativeIsEmpty() {
    TickCostSampler.Reset();
    RecordMs(3);

    Assert.Equal(0, TickCostSampler.Window(0).Count);
    Assert.Equal(0, TickCostSampler.Window(-5).Count);
  }

  [Fact]
  public void Reset_EmptiesIt() {
    TickCostSampler.Reset();
    RecordMs(1, 2, 3);
    TickCostSampler.Reset();

    Assert.Equal(0, TickCostSampler.Count);
    Assert.Equal(0, TickCostSampler.Window(10).Count);
  }

  private static void RecordMs(params double[] milliseconds) {
    foreach (double ms in milliseconds) {
      TickCostSampler.Record((long)(ms * System.Diagnostics.Stopwatch.Frequency / 1000.0));
    }
  }
}
