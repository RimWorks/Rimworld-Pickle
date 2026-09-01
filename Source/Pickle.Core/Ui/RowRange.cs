using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Ui;

/// <summary>
/// Picks the row index range a scroll view actually shows. Pure maths so it stays
/// unit testable; the drawing side cannot be, once it touches Verse.
/// </summary>
public static class RowRange {
  /// <summary>Half-open range of rows overlapping the viewport, padded one row each side.</summary>
  public static (int First, int Last) Visible(IReadOnlyList<float> rowTops, float contentHeight, float scrollY, float viewportHeight) {
    int count = rowTops.Count;
    if (count == 0 || viewportHeight <= 0f) {
      return (0, 0);
    }

    int first = 0;
    while (first < count && RowBottom(rowTops, contentHeight, first) <= scrollY) {
      first++;
    }

    int last = first;
    while (last < count && rowTops[last] < scrollY + viewportHeight) {
      last++;
    }

    // The pad covers scroll moving between the pass that picks the range and the one that draws.
    return (Math.Max(0, first - 1), Math.Min(count, last + 1));
  }

  // Rows are contiguous, so a row ends where the next begins and the last at contentHeight.
  private static float RowBottom(IReadOnlyList<float> rowTops, float contentHeight, int index) {
    return index + 1 < rowTops.Count ? rowTops[index + 1] : contentHeight;
  }
}
