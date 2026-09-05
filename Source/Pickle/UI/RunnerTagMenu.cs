using System.Collections.Generic;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

public class RunnerTagMenu : Window {
  private readonly RunnerWindow owner;
  private readonly Vector2 anchor;
  private Vector2 scroll;
  private bool multiple;

  public RunnerTagMenu(RunnerWindow owner, Vector2 anchor) {
    this.owner = owner;
    this.anchor = anchor;
    closeOnClickedOutside = true;
    doCloseX = false;
    absorbInputAroundWindow = true;
  }

  public override Vector2 InitialSize => new Vector2(370f, 380f);

  public override void DoWindowContents(Rect inRect) {
    Text.Font = GameFont.Tiny;
    Widgets.Label(new Rect(inRect.x, inRect.y, inRect.width, 48f), "Click for one tag. Shift-click to add or remove tags. Matches any selected tag and replaces scenario selection.");
    Text.Font = GameFont.Small;
    GUI.enabled = !owner.IsRunning && !Web.FixtureCommands.IsBusy;
    Widgets.CheckboxLabeled(new Rect(inRect.x, inRect.y + 52f, inRect.width, 28f), "Select multiple tags", ref multiple);
    List<string> tags = new List<string>(owner.AllTags);
    Rect list = new Rect(inRect.x, inRect.y + 88f, inRect.width, inRect.height - 126f);
    Rect content = new Rect(0f, 0f, list.width - 18f, tags.Count * 34f);
    Widgets.BeginScrollView(list, ref scroll, content);
    float y = 0f;
    foreach (string tag in tags) {
      Rect row = new Rect(0f, y, content.width, 30f);
      bool additive = multiple || Event.current.shift;
      GUI.color = owner.ActiveTagFilters.Contains(tag) ? RunnerStatusColors.Accent : Color.white;
      TooltipHandler.TipRegion(row, tag);
      if (Widgets.ButtonText(row, tag.Truncate(row.width - 16f))) {
        owner.SetFilter(tag: tag, additive: additive);
        if (!additive) {
          Close();
        }
      }

      GUI.color = Color.white;
      y += 34f;
    }

    Widgets.EndScrollView();
    GUI.enabled = GUI.enabled && owner.ActiveTagFilters.Count > 0;
    if (Widgets.ButtonText(new Rect(inRect.x, inRect.yMax - 30f, inRect.width, 30f), "Clear tag filters")) {
      owner.SetFilter(clearTags: true);
    }

    GUI.enabled = true;
  }

  protected override void SetInitialSizeAndPosition() {
    Vector2 size = InitialSize;
    windowRect = new Rect(Mathf.Clamp(anchor.x, 0f, Verse.UI.screenWidth - size.x),
        Mathf.Clamp(anchor.y + 4f, 0f, Verse.UI.screenHeight - size.y), size.x, size.y);
  }
}
