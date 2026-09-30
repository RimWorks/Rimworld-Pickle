using System;

namespace RimWorks.Pickle.Core.Ui;

/// <summary>
/// Matches the rect a widget drew in against the rect a step asked to interact with.
/// Pure maths so it stays unit testable; the widget side cannot be, once it touches Verse.
/// </summary>
public static class RectMatch {
  /// <summary>How far apart two edges may sit and still count as the same rect, in pixels.</summary>
  public const float Tolerance = 1f;

  /// <summary>Whether two rects name the same element, comparing every edge within <see cref="Tolerance"/>.</summary>
  /// <param name="rect">The rect a widget drew in.</param>
  /// <param name="other">The rect a step is waiting on.</param>
  /// <returns><c>true</c> when the two rects are the same element.</returns>
  public static bool Same(
      (float X, float Y, float Width, float Height) rect,
      (float X, float Y, float Width, float Height) other) {
    return Close(rect.X, other.X)
        && Close(rect.Y, other.Y)
        && Close(rect.Width, other.Width)
        && Close(rect.Height, other.Height);
  }

  private static bool Close(float a, float b) {
    return Math.Abs(a - b) <= Tolerance;
  }
}
