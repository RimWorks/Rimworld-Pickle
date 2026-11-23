using RimWorks.Pickle.Core.Run;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Palette and status-dot drawing for the runner. RimWorld has no built-in
/// "test passed" green to reuse, unlike the separator helpers taken from Widgets.
/// </summary>
public static class RunnerStatusColors {
  /// <summary>Highlight color for an active tab, a selected tag filter, or the running-state icon.</summary>
  public static readonly Color Accent = new Color(0.816f, 0.694f, 0.486f);

  /// <summary>Status-dot color while a run is paused.</summary>
  public static readonly Color Paused = new Color(0.890f, 0.706f, 0.365f);

  /// <summary>Status-dot color for a scenario that has not run yet.</summary>
  public static readonly Color Pending = new Color(0.353f, 0.369f, 0.392f);

  /// <summary>Status-dot color for a scenario or run that passed.</summary>
  public static readonly Color Passed = new Color(0.498f, 0.659f, 0.420f);

  /// <summary>Status-dot color for a failed scenario.</summary>
  public static readonly Color Failed = new Color(0.780f, 0.392f, 0.353f);

  /// <summary>Status-dot color for a skipped scenario.</summary>
  public static readonly Color Skipped = new Color(0.247f, 0.263f, 0.278f);

  /// <summary>Accent color for gherkin keywords, hidden-filter notices, and other secondary headings.</summary>
  public static readonly Color Keyword = new Color(0.851f, 0.604f, 0.239f);

  /// <summary>Secondary text color for labels that should not compete with the main content.</summary>
  public static readonly Color Muted = new Color(0.753f, 0.737f, 0.682f);

  /// <summary>Readable red for a failure message and the abort action.</summary>
  public static readonly Color FailedText = new Color(0.910f, 0.635f, 0.604f);

  /// <summary>Background track color of the fast and watch mode segmented toggle.</summary>
  public static readonly Color SegmentTrough = new Color(0.114f, 0.125f, 0.141f);

  /// <summary>Fill color for the selected segment of the fast and watch mode toggle.</summary>
  public static readonly Color SegmentActive = new Color(0.239f, 0.286f, 0.337f);

  /// <summary>Border color drawn around the fast and watch mode toggle.</summary>
  public static readonly Color SegmentBorder = new Color(0.267f, 0.290f, 0.318f);

  /// <summary>Picks the status-dot color for a scenario's outcome.</summary>
  /// <param name="outcome">The scenario's outcome.</param>
  /// <returns>The matching status color, or <see cref="Pending"/> for anything not passed, failed or skipped.</returns>
  public static Color ForOutcome(ScenarioOutcome outcome) {
    return outcome switch {
      ScenarioOutcome.Passed => Passed,
      ScenarioOutcome.Failed => Failed,
      ScenarioOutcome.Skipped => Skipped,
      _ => Pending,
    };
  }

  /// <summary>Picks the status-dot color for a step's status.</summary>
  /// <param name="status">The step's status.</param>
  /// <returns>The matching status color: <see cref="Failed"/> covers failed, undefined and ambiguous alike.</returns>
  public static Color ForStep(StepStatus status) {
    return status switch {
      StepStatus.Passed => Passed,
      StepStatus.Failed or StepStatus.Undefined or StepStatus.Ambiguous => Failed,
      StepStatus.Skipped => Skipped,
      _ => Pending,
    };
  }

  /// <summary>Draws a solid square status dot centered on a point.</summary>
  /// <param name="center">Where the dot is centered.</param>
  /// <param name="color">The dot's fill color.</param>
  /// <param name="size">The dot's width and height.</param>
  public static void DrawDot(Vector2 center, Color color, float size = 8f) {
    Widgets.DrawBoxSolid(new Rect(center.x - (size / 2f), center.y - (size / 2f), size, size), color);
  }
}
