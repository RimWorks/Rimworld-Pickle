using RimWorks.Pickle.Core.Ui;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class RowRangeTests {
  private const float MixedContentHeight = 108f;
  private static readonly float[] MixedTops = [0f, 26f, 48f, 68f, 88f];

  [Fact]
  public void Visible_WithNoRows_ReturnsEmpty() {
    Assert.Equal((0, 0), RowRange.Visible([], 0f, 0f, 100f));
  }

  [Fact]
  public void Visible_WithZeroViewport_ReturnsEmpty() {
    Assert.Equal((0, 0), RowRange.Visible(MixedTops, MixedContentHeight, 0f, 0f));
  }

  [Fact]
  public void Visible_WhenEverythingFits_ReturnsAllRows() {
    Assert.Equal((0, 5), RowRange.Visible(MixedTops, MixedContentHeight, 0f, 200f));
  }

  [Fact]
  public void Visible_MidScroll_ReturnsOverlapPlusPad() {
    // Viewport [50, 90): rows 2..4 overlap, padded to (1, 5).
    Assert.Equal((1, 5), RowRange.Visible(MixedTops, MixedContentHeight, 50f, 40f));
  }

  [Fact]
  public void Visible_ScrolledPastEnd_ClampsToCount() {
    Assert.Equal((4, 5), RowRange.Visible(MixedTops, MixedContentHeight, 200f, 40f));
  }

  [Fact]
  public void Visible_WithNegativeScroll_StartsAtFirstRow() {
    Assert.Equal((0, 2), RowRange.Visible(MixedTops, MixedContentHeight, -10f, 30f));
  }

  [Fact]
  public void Visible_RowTouchingViewportBottom_IsExcludedBeyondPad() {
    // Viewport [0, 26): row 0 overlaps, row 1 starts exactly at the edge, pad adds it.
    Assert.Equal((0, 2), RowRange.Visible(MixedTops, MixedContentHeight, 0f, 26f));
  }
}
