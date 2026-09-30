using RimWorks.Pickle.Core.Ui;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class RectMatchTests {
  private static readonly (float X, float Y, float Width, float Height) Button = (10f, 20f, 100f, 30f);

  [Fact]
  public void Same_WithIdenticalRects_Matches() {
    Assert.True(RectMatch.Same(Button, (10f, 20f, 100f, 30f)));
  }

  [Fact]
  public void Same_WithinTolerance_Matches() {
    Assert.True(RectMatch.Same(Button, (10.5f, 19.5f, 100.4f, 30.6f)));
  }

  [Fact]
  public void Same_AtExactlyTolerance_Matches() {
    Assert.True(RectMatch.Same(Button, (11f, 21f, 101f, 31f)));
  }

  [Fact]
  public void Same_WithShiftedPosition_DoesNotMatch() {
    Assert.False(RectMatch.Same(Button, (14f, 20f, 100f, 30f)));
  }

  [Fact]
  public void Same_WithSharedOriginButDifferentSize_DoesNotMatch() {
    Assert.False(RectMatch.Same(Button, (10f, 20f, 100f, 300f)));
  }

  [Fact]
  public void Same_WithOneEdgeJustOutsideTolerance_DoesNotMatch() {
    Assert.False(RectMatch.Same(Button, (10f, 20f, 100f, 31.5f)));
  }
}
