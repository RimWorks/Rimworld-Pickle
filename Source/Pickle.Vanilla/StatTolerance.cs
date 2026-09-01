using System;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// How close a stat has to be to count as equal. A flat tolerance breaks on MarketValue
/// in the thousands and a relative one breaks near zero, so a comparison takes whichever
/// is looser.
/// </summary>
internal static class StatTolerance {
  private const float Absolute = 0.01f;
  private const float Relative = 0.001f;

  public static float For(float expected) {
    return Math.Max(Absolute, Math.Abs(expected) * Relative);
  }

  public static bool IsNear(float actual, float expected) {
    return Math.Abs(actual - expected) <= For(expected);
  }
}
