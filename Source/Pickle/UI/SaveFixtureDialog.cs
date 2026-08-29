using System;
using System.Collections.Generic;
using System.IO;
using Pickle.Core.Discovery;
using RimWorld;
using UnityEngine;
using Verse;

namespace Pickle.UI;

/// <summary>
/// Saves the running game into a mod's Pickle/Fixtures/. Saves to a scratch name first,
/// then copies, so a half-written file never lands in a suite.
/// </summary>
public class SaveFixtureDialog : Window {
  private const string ScratchSaveName = "pickle_fixture_scratch";
  private const float RowHeight = 30f;

  private readonly List<DiscoveredSuite> suites;
  private string fixtureName = string.Empty;
  private int selectedSuite;

  public SaveFixtureDialog(List<DiscoveredSuite> suites) {
    this.suites = suites;

    optionalTitle = "Pickle_SaveFixtureTitle".Translate();
    draggable = true;
    doCloseX = true;
    closeOnClickedOutside = true;
    absorbInputAroundWindow = true;
  }

  /// <inheritdoc/>
  public override Vector2 InitialSize => new Vector2(480f, 160f + (suites.Count * RowHeight));

  /// <inheritdoc/>
  public override void DoWindowContents(Rect inRect) {
    float y = inRect.y;

    Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "Pickle_FixtureName".Translate());
    y += 24f;
    fixtureName = Widgets.TextField(new Rect(inRect.x, y, inRect.width, 28f), fixtureName);
    y += 36f;

    Widgets.Label(new Rect(inRect.x, y, inRect.width, 24f), "Pickle_FixtureMod".Translate());
    y += 24f;

    for (int i = 0; i < suites.Count; i++) {
      Rect row = new Rect(inRect.x, y, inRect.width, RowHeight);
      if (Widgets.RadioButtonLabeled(row, suites[i].ModName, selectedSuite == i)) {
        selectedSuite = i;
      }

      y += RowHeight;
    }

    y += 8f;

    bool canSave = Current.Game != null && !fixtureName.NullOrEmpty() && suites.Count > 0;
    GUI.enabled = canSave;
    if (Widgets.ButtonText(new Rect(inRect.x, y, 120f, 30f), "Pickle_Save".Translate())) {
      Save(suites[selectedSuite], fixtureName.Trim());
      Close();
    }

    GUI.enabled = true;

    if (Current.Game == null) {
      GUI.color = RunnerStatusColors.FailedText;
      Widgets.Label(new Rect(inRect.x + 132f, y, inRect.width - 132f, 30f), "Pickle_LoadGameFirst".Translate());
      GUI.color = Color.white;
    }
  }

  internal static void Save(DiscoveredSuite suite, string name) {
    string scratch = GenFilePaths.FilePathForSavedGame(ScratchSaveName);

    try {
      GameDataSaveLoader.SaveGame(ScratchSaveName);

      // SaveGame swallows its own exceptions into a log line, so the file is the
      // only honest signal that it worked.
      if (!File.Exists(scratch)) {
        Messages.Message("Pickle_SaveFailed".Translate(), MessageTypeDefOf.RejectInput, false);
        return;
      }

      Directory.CreateDirectory(suite.FixturesDir);
      string target = Path.Combine(suite.FixturesDir, name + ".rws");
      File.Copy(scratch, target, overwrite: true);

      Messages.Message("Pickle_SavedFixtureTo".Translate(target), MessageTypeDefOf.TaskCompletion, false);
    } catch (Exception ex) {
      Log.Error($"pickle: save fixture failed: {ex}");
      Messages.Message("Pickle_SaveFixtureFailed".Translate(), MessageTypeDefOf.RejectInput, false);
    } finally {
      try {
        File.Delete(scratch);
      } catch {
        // best effort cleanup; the save already succeeded or already logged its failure
      }
    }
  }
}
