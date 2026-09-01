using RimWorks.Pickle.Core.Run;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Palette and status-dot drawing for the runner. RimWorld has no built-in
/// "test passed" green to reuse, unlike the separator helpers taken from Widgets.
/// </summary>
public static class RunnerStatusColors {
  public static readonly Color Pending = new Color(0.353f, 0.369f, 0.392f);
  public static readonly Color Passed = new Color(0.498f, 0.659f, 0.420f);
  public static readonly Color Failed = new Color(0.780f, 0.392f, 0.353f);
  public static readonly Color Skipped = new Color(0.247f, 0.263f, 0.278f);
  public static readonly Color Keyword = new Color(0.851f, 0.604f, 0.239f);
  public static readonly Color Muted = new Color(0.478f, 0.502f, 0.533f);
  public static readonly Color FailedText = new Color(0.910f, 0.635f, 0.604f);
  public static readonly Color SegmentTrough = new Color(0.114f, 0.125f, 0.141f);
  public static readonly Color SegmentActive = new Color(0.239f, 0.286f, 0.337f);
  public static readonly Color SegmentBorder = new Color(0.267f, 0.290f, 0.318f);

  public static Color ForOutcome(ScenarioOutcome outcome) {
    return outcome switch {
      ScenarioOutcome.Passed => Passed,
      ScenarioOutcome.Failed => Failed,
      ScenarioOutcome.Skipped => Skipped,
      _ => Pending,
    };
  }

  public static Color ForStep(StepStatus status) {
    return status switch {
      StepStatus.Passed => Passed,
      StepStatus.Failed => Failed,
      StepStatus.Skipped => Skipped,
      _ => Pending,
    };
  }

  public static void DrawDot(Vector2 center, Color color, float size = 8f) {
    Widgets.DrawBoxSolid(new Rect(center.x - (size / 2f), center.y - (size / 2f), size, size), color);
  }

  // Widgets.Label clips at the Rect edge instead of ellipsizing, which reads as broken.
  // Trim to the widest prefix that fits.
  public static string Ellipsize(string text, float maxWidth) {
    if (Text.CalcSize(text).x <= maxWidth) {
      return text;
    }

    for (int length = text.Length - 1; length > 0; length--) {
      string candidate = text[..length] + "…";
      if (Text.CalcSize(candidate).x <= maxWidth) {
        return candidate;
      }
    }

    return "…";
  }
}
