using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Search box, tag chips, and mod picker. Chips are built from whatever tags the
/// discovered scenarios carry, so an untagged suite renders an empty row.
/// </summary>
public static class RunnerFilterBar {
  private const float SearchWidth = 240f;
  private const float ChipHeight = 20f;
  private const float ChipPadding = 6f;
  private const float ModPickerWidth = 140f;

  public static float Height(float width, RunnerWindow window) {
    float x = 4f;
    float height = 30f;
    foreach (string tag in window.AllTags) {
      float chipWidth = Mathf.Min(ChipWidth(tag), width - 8f);
      if (x + chipWidth > width - 4f) {
        height += 26f;
        x = 4f;
      }

      x += chipWidth + ChipPadding;
    }

    return height + (x > 4f ? 26f : 0f);
  }

  public static void Draw(Rect rect, RunnerWindow window) {
    float x = rect.x + 4f;
    Rect searchRect = new Rect(x, rect.y + 5f, Mathf.Min(SearchWidth, (rect.width * 0.6f) - 8f), ChipHeight);
    string search = Widgets.TextField(searchRect, window.SearchText);
    if (search != window.SearchText) {
      window.SetFilter(search: search);
    }

    x = rect.x + 4f;
    float chipY = rect.y + 32f;
    foreach (string tag in window.AllTags) {
      float chipWidth = Mathf.Min(ChipWidth(tag), rect.width - 8f);
      if (x + chipWidth > rect.xMax - 4f) {
        x = rect.x + 4f;
        chipY += 26f;
      }
      Rect chipRect = new Rect(x, chipY, chipWidth, ChipHeight);
      bool active = window.ActiveTagFilters.Contains(tag);
      DrawChip(chipRect, tag, active);
      if (Widgets.ButtonInvisible(chipRect)) {
        window.SetFilter(tag: tag);
      }

      x += chipWidth + ChipPadding;
    }

    string modLabel = window.ModFilterSelection ?? "all";
    string modChipText = $"mods: {modLabel} ▾";
    float modChipWidth = Mathf.Min((rect.width * 0.4f) - 8f, Mathf.Max(ModPickerWidth, Text.CalcSize(modChipText).x + (ChipPadding * 2f)));
    Rect modRect = new Rect(rect.xMax - modChipWidth - 4f, rect.y + 5f, modChipWidth, ChipHeight);
    DrawChip(modRect, modChipText.Truncate(modChipWidth - (ChipPadding * 2f)), false);
    if (Widgets.ButtonInvisible(modRect)) {
      OpenModPicker(window);
    }
  }

  private static float ChipWidth(string tag) {
    Text.Font = GameFont.Tiny;
    float width = Text.CalcSize(tag).x + (ChipPadding * 2f);
    Text.Font = GameFont.Small;
    return width;
  }

  private static void DrawChip(Rect rect, string label, bool active) {
    Color outline = active ? RunnerStatusColors.Keyword : Widgets.SeparatorLineColor;
    Color fill = active ? new Color(0.851f, 0.604f, 0.239f, 0.15f) : Color.clear;
    Widgets.DrawBoxSolidWithOutline(rect, fill, outline);

    Text.Font = GameFont.Tiny;
    Text.Anchor = TextAnchor.MiddleCenter;
    GUI.color = active ? RunnerStatusColors.Keyword : Color.white;
    Widgets.Label(rect, label);
    GUI.color = Color.white;
    Text.Anchor = TextAnchor.UpperLeft;
    Text.Font = GameFont.Small;
  }

  private static void OpenModPicker(RunnerWindow window) {
    List<FloatMenuOption> options =
    [
        new FloatMenuOption("all mods", () => window.SetFilter(mod: string.Empty)),
        ];

    foreach (string modName in window.AllModNames) {
      options.Add(new FloatMenuOption(modName, () => window.SetFilter(mod: modName)));
    }

    Find.WindowStack.Add(new FloatMenu(options));
  }
}
