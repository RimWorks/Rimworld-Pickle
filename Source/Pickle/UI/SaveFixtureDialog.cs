using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Fixtures;
using RimWorks.Pickle.Web;
using RimWorld;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Saves the running game into a mod's Pickle/Fixtures/. Saves to a scratch name first,
/// then copies, so a half-written file never lands in a suite.
/// </summary>
public class SaveFixtureDialog : Window {
  private const float RowHeight = 30f;

  private readonly List<DiscoveredSuite> suites;
  private readonly Action? onSaved;
  private string fixtureName = string.Empty;
  private int selectedSuite;

  public SaveFixtureDialog(List<DiscoveredSuite> suites, Action? onSaved = null) {
    this.suites = suites;
    this.onSaved = onSaved;

    optionalTitle = "Pickle_SaveFixtureTitle".Translate();
    draggable = true;
    doCloseX = true;
    closeOnClickedOutside = true;
    absorbInputAroundWindow = true;
  }

  // 210, not 160: the window margins and the title row take 79px before DoWindowContents
  // sees the rect, and the Save button is the last thing laid out, so it is what clips.

  /// <inheritdoc/>
  public override Vector2 InitialSize => new Vector2(480f, 210f + (suites.Count * RowHeight));

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

    bool validName;
    try {
      _ = FixtureCatalog.PathForName(string.Empty, fixtureName.Trim());
      validName = true;
    } catch (ArgumentException) {
      validName = false;
    }

    bool canSave = Current.Game != null && validName && suites.Count > 0 && !RunnerWindow.Instance.IsRunning && !FixtureCommands.IsBusy;
    GUI.enabled = canSave;
    if (Widgets.ButtonText(new Rect(inRect.x, y, 120f, 30f), "Pickle_Save".Translate())) {
      DiscoveredSuite suite = suites[selectedSuite];
      string target = FixtureCatalog.PathForName(suite.WritableFixturesDir, fixtureName.Trim());
      if (File.Exists(target)) {
        Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation($"Overwrite fixture '{fixtureName.Trim()}'?\n{target}", () => _ = SaveAndClose(true), destructive: true));
      } else {
        _ = SaveAndClose(false);
      }
    }

    GUI.enabled = true;

    if (Current.Game == null) {
      GUI.color = RunnerStatusColors.FailedText;
      Widgets.Label(new Rect(inRect.x + 132f, y, inRect.width - 132f, 30f), "Pickle_LoadGameFirst".Translate());
      GUI.color = Color.white;
    }
  }

  internal static bool Save(DiscoveredSuite suite, string name, bool overwrite = false) {
    string scratchName = "pickle_fixture_" + Guid.NewGuid().ToString("N");
    string scratch = GenFilePaths.FilePathForSavedGame(scratchName);

    try {
      string target = FixtureCatalog.PathForName(suite.WritableFixturesDir, name);
      if (Current.Game == null) {
        return false;
      }

      GameDataSaveLoader.SaveGame(scratchName);

      // SaveGame swallows its own exceptions into a log line, so the file is the
      // only honest signal that it worked.
      if (!File.Exists(scratch)) {
        Messages.Message("Pickle_SaveFailed".Translate(), MessageTypeDefOf.RejectInput, false);
        return false;
      }

      Directory.CreateDirectory(suite.WritableFixturesDir);
      File.Copy(scratch, target, overwrite);

      Messages.Message("Pickle_SavedFixtureTo".Translate(target), MessageTypeDefOf.TaskCompletion, false);
      return true;
    } catch (Exception ex) {
      Log.Error(ex, "pickle: save fixture failed");
      Messages.Message("Pickle_SaveFixtureFailed".Translate(), MessageTypeDefOf.RejectInput, false);
      return false;
    } finally {
      try {
        File.Delete(scratch);
      } catch {
        // best effort cleanup; the save already succeeded or already logged its failure
      }
    }
  }

  private async Task SaveAndClose(bool overwrite) {
    try {
      await FixtureCommands.Execute("save", suites[selectedSuite].FixturesDir, fixtureName.Trim(), null, overwrite);
      onSaved?.Invoke();
      Close();
    } catch (Exception ex) {
      Messages.Message(ex.Message, MessageTypeDefOf.RejectInput, false);
    }
  }
}
