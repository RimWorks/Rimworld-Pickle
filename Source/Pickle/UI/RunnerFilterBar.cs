using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

public static class RunnerFilterBar {
  private const float RowHeight = 40f;
  private const float Padding = 8f;

  public static float Height(float width, RunnerWindow window) {
    float fieldsWidth = FieldsWidth(width);
    float x = 70f;
    float height = RowHeight;
    foreach (string tag in window.ActiveTagFilters) {
      float chipWidth = ChipWidth(tag, fieldsWidth);
      if (x + chipWidth > fieldsWidth) {
        height += 30f;
        x = 0f;
      }

      x += chipWidth + 6f;
    }

    if (window.ActiveTagFilters.Count > 0) {
      height += 30f;
    }

    return height + (width < 1000f ? RowHeight : 0f);
  }

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
    string tagLabel = window.ActiveTagFilters.Count == 0 ? "Select by tag" : $"{window.ActiveTagFilters.Count} tags · match any";
    if (Widgets.ButtonText(tagRect, tagLabel)) {
      Find.WindowStack.Add(new RunnerTagMenu(window, GUIUtility.GUIToScreenPoint(new Vector2(tagRect.x, tagRect.yMax))));
    }

    float x = rect.x + Padding + 70f;
    float y = rect.y + RowHeight;
    Text.Font = GameFont.Tiny;
    if (window.ActiveTagFilters.Count > 0) {
      Widgets.Label(new Rect(rect.x + Padding, y + 5f, 65f, 24f), "Match any");
    }

    foreach (string tag in new List<string>(window.ActiveTagFilters)) {
      float chipWidth = ChipWidth(tag, fieldsWidth);
      if (x + chipWidth > rect.x + Padding + fieldsWidth) {
        x = rect.x + Padding;
        y += 30f;
      }

      Rect chip = new Rect(x, y + 2f, chipWidth, 26f);
      TooltipHandler.TipRegion(chip, $"Remove {tag} tag");
      if (Widgets.ButtonText(chip, tag.Truncate(chipWidth - 28f))) {
        window.SetFilter(tag: tag, additive: true);
      }

      Vector2 c = new Vector2(chip.xMax - 11f, chip.center.y);
      Widgets.DrawLine(c + new Vector2(-3f, -3f), c + new Vector2(3f, 3f), RunnerStatusColors.Muted, 1f);
      Widgets.DrawLine(c + new Vector2(3f, -3f), c + new Vector2(-3f, 3f), RunnerStatusColors.Muted, 1f);
      x += chipWidth + 6f;
    }

    Text.Font = GameFont.Small;
    GUI.enabled = true;
    RunnerToolbar.DrawActions(new Rect(rect.x, rect.yMax - 35f, rect.width - Padding, 30f), window);
  }

  private static float FieldsWidth(float width) {
    return width - (Padding * 2f) - (width < 1000f ? 0f : RunnerToolbar.ActionsWidth + 18f);
  }

  private static float ChipWidth(string tag, float fieldsWidth) {
    Text.Font = GameFont.Tiny;
    float width = Mathf.Min(Text.CalcSize(tag).x + 32f, fieldsWidth);
    Text.Font = GameFont.Small;
    return width;
  }
}
