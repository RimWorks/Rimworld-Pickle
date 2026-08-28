using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Verse;

namespace Pickle.UI;

/// <summary>
/// Search box, tag chips, and mod picker. Chips are built from whatever tags the
/// discovered scenarios carry, so an untagged suite renders an empty row.
/// </summary>
public static class RunnerFilterBar {
  private const float SearchWidth = 240f;
  private const float ChipHeight = 20f;
  private const float ChipPadding = 6f;
  private const float ModPickerWidth = 140f;

  public static void Draw(Rect rect, RunnerWindow window) {
    float x = rect.x + 4f;
    Rect searchRect = new Rect(x, rect.y + ((rect.height - ChipHeight) / 2f), SearchWidth, ChipHeight);
    window.SearchText = Widgets.TextField(searchRect, window.SearchText);

    x += SearchWidth + 10f;
    float chipY = rect.y + ((rect.height - ChipHeight) / 2f);
    foreach (string tag in window.AllTags) {
      float chipWidth = Text.CalcSize(tag).x + (ChipPadding * 2f);
      Rect chipRect = new Rect(x, chipY, chipWidth, ChipHeight);
      bool active = window.ActiveTagFilters.Contains(tag);
      DrawChip(chipRect, tag, active);
      if (Widgets.ButtonInvisible(chipRect)) {
        if (active) {
          window.ActiveTagFilters.Remove(tag);
        } else {
          window.ActiveTagFilters.Add(tag);
        }
      }

      x += chipWidth + ChipPadding;
    }

    string modLabel = window.ModFilterSelection ?? "all";
    string modChipText = $"mods: {modLabel} ▾";
    float modChipWidth = Mathf.Max(ModPickerWidth, Text.CalcSize(modChipText).x + (ChipPadding * 2f));
    Rect modRect = new Rect(rect.xMax - modChipWidth - 4f, chipY, modChipWidth, ChipHeight);
    DrawChip(modRect, modChipText, false);
    if (Widgets.ButtonInvisible(modRect)) {
      OpenModPicker(window);
    }
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
        new FloatMenuOption("all mods", () => window.ModFilterSelection = null),
        ];

    foreach (string modName in window.AllModNames) {
      options.Add(new FloatMenuOption(modName, () => window.ModFilterSelection = modName));
    }

    Find.WindowStack.Add(new FloatMenu(options));
  }
}
