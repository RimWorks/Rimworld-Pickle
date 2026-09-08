using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>The runner window's search, mod and tag filter row, including the removable tag chips.</summary>
public static class RunnerFilterBar {
  private const float RowHeight = 40f;
  private const float Padding = 8f;

  private const float ChipPadding = 20f;
  private const float ChipHeight = 30f;
  private const float ChipRowStep = 34f;

  // The cross rides in the label rather than being drawn over it. Two DrawLine calls got
  // clipped at the chip's midline and left a half cross behind.
  private const string ChipSuffix = "  \u00d7";

  /// <summary>How tall the bar needs to be, which grows as the tag chips wrap onto more rows.</summary>
  /// <param name="width">The window width the bar has to fit.</param>
  /// <param name="window">The runner window whose active filters are drawn.</param>
  /// <returns>The height in pixels.</returns>
  public static float Height(float width, RunnerWindow window) {
    float fieldsWidth = FieldsWidth(width);
    float x = 70f;
    float height = RowHeight;
    foreach (string tag in window.ActiveTagFilters) {
      float chipWidth = ChipWidth(tag, fieldsWidth);
      if (x + chipWidth > fieldsWidth) {
        height += ChipRowStep;
        x = 0f;
      }

      x += chipWidth + 6f;
    }

    if (window.ActiveTagFilters.Count > 0) {
      height += ChipRowStep;
    }

    return height + (width < 1000f ? RowHeight : 0f);
  }

  /// <summary>Draws the bar and writes any change straight back onto the window's filters.</summary>
  /// <param name="rect">The area to draw into.</param>
  /// <param name="window">The runner window whose filters are drawn and edited.</param>
  public static void Draw(Rect rect, RunnerWindow window) {
    float fieldsWidth = FieldsWidth(rect.width);
    float searchWidth = Mathf.Max(100f, fieldsWidth - 300f);
    Rect searchRect = new Rect(rect.x + Padding, rect.y + 5f, searchWidth, 30f);
    string search = Widgets.TextField(searchRect, window.SearchText);
    if (search != window.SearchText) {
      window.SetFilter(search: search);
    }

    Rect modRect = new Rect(searchRect.xMax + 8f, searchRect.y, 138f, 30f);
    string modLabel = window.ModFilterSelection ?? "All mods";
    if (Widgets.ButtonText(modRect, modLabel.Truncate(modRect.width - 12f))) {
      List<FloatMenuOption> options = [new FloatMenuOption("All mods", () => window.SetFilter(mod: string.Empty))];
      foreach (string mod in window.AllModNames) {
        options.Add(new FloatMenuOption(mod, () => window.SetFilter(mod: mod)));
      }

      Find.WindowStack.Add(new FloatMenu(options));
    }

    Rect tagRect = new Rect(modRect.xMax + 8f, searchRect.y, 144f, 30f);
    GUI.enabled = !window.IsRunning && !Web.FixtureCommands.IsBusy;
    string tagLabel = window.ActiveTagFilters.Count == 0 ? "Select by tag" : $"{window.ActiveTagFilters.Count} tags · match all";
    if (Widgets.ButtonText(tagRect, tagLabel)) {
      Find.WindowStack.Add(new RunnerTagMenu(window, GUIUtility.GUIToScreenPoint(new Vector2(tagRect.x, tagRect.yMax))));
    }

    float x = rect.x + Padding + 70f;
    float y = rect.y + RowHeight;
    Text.Font = GameFont.Tiny;
    if (window.ActiveTagFilters.Count > 0) {
      Widgets.Label(new Rect(rect.x + Padding, y + 5f, 65f, 24f), "Match all");
    }

    foreach (string tag in new List<string>(window.ActiveTagFilters)) {
      float chipWidth = ChipWidth(tag, fieldsWidth);
      if (x + chipWidth > rect.x + Padding + fieldsWidth) {
        x = rect.x + Padding;
        y += ChipRowStep;
      }

      Rect chip = new Rect(x, y + 2f, chipWidth, ChipHeight);
      TooltipHandler.TipRegion(chip, $"Remove {tag} tag");
      if (Widgets.ButtonText(chip, (tag + ChipSuffix).Truncate(chipWidth))) {
        window.SetFilter(tag: tag, additive: true);
      }

      x += chipWidth + 6f;
    }

    Text.Font = GameFont.Small;
    GUI.enabled = true;
    RunnerToolbar.DrawActions(new Rect(rect.x, rect.yMax - 35f, rect.width - Padding, 30f), window);
  }

  private static float FieldsWidth(float width) {
    return width - (Padding * 2f) - (width < 1000f ? 0f : RunnerToolbar.ActionsWidth + 18f);
  }

  // Restores whatever the caller was using. Hardcoding Small here measured the tag in Tiny
  // and then drew it in Small, so every chip came out too narrow for its own label.
  private static float ChipWidth(string tag, float fieldsWidth) {
    GameFont previous = Text.Font;
    Text.Font = GameFont.Tiny;
    float width = Mathf.Min(Text.CalcSize(tag + ChipSuffix).x + ChipPadding, fieldsWidth);
    Text.Font = previous;
    return width;
  }
}
