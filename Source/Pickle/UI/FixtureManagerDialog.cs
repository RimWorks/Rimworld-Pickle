using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Fixtures;
using RimWorks.Pickle.Fixtures;
using RimWorks.Pickle.Runtime;
using RimWorld;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.UI;

/// <summary>
/// Lists every fixture Pickle can see, grouped by the mod that owns it, and loads, renames
/// or deletes one. Without it a recorded .rws is write-only from inside the game.
/// </summary>
public class FixtureManagerDialog : Window {
  private const float HeaderHeight = 36f;
  private const float SaveButtonWidth = 128f;
  private const float RowHeight = 46f;
  private const float GroupHeaderHeight = 30f;
  private const float ActionWidth = 66f;
  private const float ActionGap = 6f;
  private const float ActionsWidth = (ActionWidth * 3f) + (ActionGap * 2f);

  private readonly List<(DiscoveredSuite Suite, List<FixtureEntry> Entries)> groups = [];
  private readonly Dictionary<string, FixtureHeader> headers = new(StringComparer.Ordinal);

  private Vector2 scroll;
  private string? renamingPath;
  private string renameText = string.Empty;

  public FixtureManagerDialog() {
    optionalTitle = "Pickle_FixtureManagerTitle".Translate();
    draggable = true;
    resizeable = true;
    doCloseX = true;
    closeOnClickedOutside = false;

    Refresh();
  }

  /// <inheritdoc/>
  public override Vector2 InitialSize => new Vector2(940f, 600f);

  /// <inheritdoc/>
  public override void DoWindowContents(Rect inRect) {
    DrawHeader(new Rect(inRect.x, inRect.y, inRect.width, HeaderHeight));
    Rect listRect = new Rect(inRect.x, inRect.y + HeaderHeight, inRect.width, inRect.height - HeaderHeight);

    if (groups.Count == 0) {
      GUI.color = RunnerStatusColors.Muted;
      Widgets.Label(listRect, "Pickle_NoSuites".Translate());
      GUI.color = Color.white;
      return;
    }

    float contentHeight = 0f;
    foreach ((DiscoveredSuite _, List<FixtureEntry> entries) in groups) {
      contentHeight += GroupHeaderHeight + (Math.Max(entries.Count, 1) * RowHeight);
    }

    Rect viewRect = new Rect(0f, 0f, listRect.width - 20f, contentHeight);
    Widgets.BeginScrollView(listRect, ref scroll, viewRect);

    float y = 0f;
    foreach ((DiscoveredSuite suite, List<FixtureEntry> entries) in groups) {
      DrawGroupHeader(new Rect(0f, y, viewRect.width, GroupHeaderHeight), suite);
      y += GroupHeaderHeight;

      if (entries.Count == 0) {
        DrawEmptyGroup(new Rect(0f, y, viewRect.width, RowHeight), suite);
        y += RowHeight;
        continue;
      }

      foreach (FixtureEntry entry in entries) {
        DrawRow(new Rect(0f, y, viewRect.width, RowHeight), entry, suite.ModName);
        y += RowHeight;
      }
    }

    Widgets.EndScrollView();
  }

  private static void DrawGroupHeader(Rect rect, DiscoveredSuite suite) {
    Text.Anchor = TextAnchor.LowerLeft;
    Widgets.Label(new Rect(rect.x + 2f, rect.y, rect.width - 4f, rect.height - 4f), suite.ModName);
    Text.Anchor = TextAnchor.UpperLeft;
    Widgets.DrawLineHorizontal(rect.x, rect.yMax - 2f, rect.width, Widgets.SeparatorLineColor);
  }

  private static void DrawEmptyGroup(Rect rect, DiscoveredSuite suite) {
    Text.Font = GameFont.Tiny;
    GUI.color = RunnerStatusColors.Muted;
    Widgets.Label(
        new Rect(rect.x + 8f, rect.y + 8f, rect.width - 16f, 20f),
        "Pickle_NoFixturesIn".Translate(suite.WritableFixturesDir));
    GUI.color = Color.white;
    Text.Font = GameFont.Small;
  }

  private static string TargetPath(FixtureEntry entry, string name) {
    return Path.Combine(Path.GetDirectoryName(entry.FullPath) ?? string.Empty, name + ".rws");
  }

  private static string? RenameProblem(FixtureEntry entry, string proposed) {
    string trimmed = proposed.Trim();

    if (trimmed.Length == 0) {
      return "Pickle_RenameEmpty".Translate();
    }

    if (trimmed.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0) {
      return "Pickle_RenameInvalid".Translate();
    }

    if (string.Equals(trimmed, entry.Name, StringComparison.Ordinal)) {
      return null;
    }

    if (File.Exists(TargetPath(entry, trimmed))) {
      return "Pickle_RenameTaken".Translate();
    }

    return null;
  }

  private static string Tooltip(FixtureEntry entry) {
    string where = entry.IsRecorded ? "Pickle_RecordedTip".Translate() : "Pickle_CommittedTip".Translate();
    string tip = $"{entry.FullPath}\n\n{where}";

    // The copy that loses gets no row, so its path only exists here.
    if (entry.ShadowedPath != null) {
      tip += "\n\n" + "Pickle_ShadowedPathTip".Translate(entry.ShadowedPath);
    }

    return tip;
  }

  private static string FormatSize(long bytes) {
    if (bytes >= 1024L * 1024L) {
      return ((float)bytes / 1024f / 1024f).ToString("0.#", CultureInfo.InvariantCulture) + " MB";
    }

    if (bytes >= 1024L) {
      return ((float)bytes / 1024f).ToString("0", CultureInfo.InvariantCulture) + " KB";
    }

    return bytes.ToString(CultureInfo.InvariantCulture) + " B";
  }

  // The scene swap clears the window stack, so nothing here survives to report on it.
  // A failure lands in the log and as a message on whatever comes back.
  private static async Task LoadAsync(string path) {
    try {
      await FixtureLoader.LoadFixture(path, PickleDriver.Instance);
    } catch (Exception ex) {
      Log.Error(ex, "pickle: loading fixture failed");
      Messages.Message("Pickle_LoadFixtureFailed".Translate(), MessageTypeDefOf.RejectInput, false);
    }
  }

  private void DrawHeader(Rect rect) {
    Rect saveRect = new Rect(rect.xMax - SaveButtonWidth, rect.y + 2f, SaveButtonWidth, 28f);

    GUI.enabled = groups.Count > 0;
    if (Widgets.ButtonText(saveRect, "Pickle_SaveFixture".Translate())) {
      Find.WindowStack.Add(new SaveFixtureDialog([.. groups.Select(g => g.Suite)], Refresh));
    }

    GUI.enabled = true;

    if (Current.Game == null) {
      Text.Font = GameFont.Tiny;
      GUI.color = RunnerStatusColors.Muted;
      Text.Anchor = TextAnchor.MiddleRight;
      Widgets.Label(new Rect(rect.x, rect.y + 2f, rect.width - SaveButtonWidth - 8f, 28f), "Pickle_LoadGameFirst".Translate());
      Text.Anchor = TextAnchor.UpperLeft;
      GUI.color = Color.white;
      Text.Font = GameFont.Small;
    }

    Widgets.DrawLineHorizontal(rect.x, rect.yMax - 2f, rect.width, Widgets.SeparatorLineColor);
  }

  private void DrawRow(Rect rect, FixtureEntry entry, string modName) {
    if (Mouse.IsOver(rect)) {
      Widgets.DrawHighlight(rect);
    }

    TooltipHandler.TipRegion(rect, Tooltip(entry));

    Rect actions = new Rect(rect.xMax - ActionsWidth, rect.y + 9f, ActionsWidth, 28f);
    Rect nameRect = new Rect(rect.x + 2f, rect.y + 3f, rect.width - ActionsWidth - 10f, 24f);
    Rect detailRect = new Rect(rect.x + 2f, rect.y + 25f, rect.width - ActionsWidth - 10f, 18f);

    if (renamingPath == entry.FullPath) {
      DrawRenameRow(nameRect, detailRect, actions, entry);
      return;
    }

    Widgets.Label(nameRect, entry.Name);

    DrawDetailLine(detailRect, entry, modName);
    DrawActions(actions, entry);
  }

  private void DrawDetailLine(Rect rect, FixtureEntry entry, string modName) {
    headers.TryGetValue(entry.FullPath, out FixtureHeader? header);

    // Owner and status first: a line that has to truncate should lose the game version,
    // not the part that says no run will ever load this copy.
    List<string> parts = [
      modName,
      entry.IsRecorded ? "Pickle_SourceRecorded".Translate() : "Pickle_SourceCommitted".Translate(),
    ];

    if (entry.ShadowedPath != null) {
      parts.Add("Pickle_ShadowsCommitted".Translate());
    }

    parts.Add(FormatSize(entry.SizeBytes));
    parts.Add(entry.Modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture));

    if (!string.IsNullOrEmpty(header?.ScenarioName)) {
      parts.Add(header!.ScenarioName!);
    }

    if (!string.IsNullOrEmpty(header?.GameVersion)) {
      parts.Add(header!.GameVersion!);
    }

    Text.Font = GameFont.Tiny;
    GUI.color = entry.ShadowedPath != null ? RunnerStatusColors.Keyword : RunnerStatusColors.Muted;

    // Truncated, not clipped: Widgets.Label cuts mid-word with no sign anything was lost.
    Widgets.Label(rect, string.Join(" · ", parts).Truncate(rect.width));
    GUI.color = Color.white;
    Text.Font = GameFont.Small;
  }

  private void DrawActions(Rect rect, FixtureEntry entry) {
    float x = rect.xMax - ActionWidth;
    if (Widgets.ButtonText(new Rect(x, rect.y, ActionWidth, rect.height), "Pickle_Delete".Translate())) {
      ConfirmDelete(entry);
    }

    x -= ActionWidth + ActionGap;
    if (Widgets.ButtonText(new Rect(x, rect.y, ActionWidth, rect.height), "Pickle_Rename".Translate())) {
      renamingPath = entry.FullPath;
      renameText = entry.Name;
    }

    x -= ActionWidth + ActionGap;
    GUI.enabled = !RunnerWindow.Instance.IsRunning;
    if (Widgets.ButtonText(new Rect(x, rect.y, ActionWidth, rect.height), "Pickle_Load".Translate())) {
      Close();
      _ = LoadAsync(entry.FullPath);
    }

    GUI.enabled = true;
  }

  private void DrawRenameRow(Rect nameRect, Rect detailRect, Rect actions, FixtureEntry entry) {
    renameText = Widgets.TextField(new Rect(nameRect.x, nameRect.y, nameRect.width, 26f), renameText);

    string? problem = RenameProblem(entry, renameText);
    if (problem != null) {
      Text.Font = GameFont.Tiny;
      GUI.color = RunnerStatusColors.FailedText;
      Widgets.Label(detailRect, problem);
      GUI.color = Color.white;
      Text.Font = GameFont.Small;
    }

    float x = actions.xMax - ActionWidth;
    if (Widgets.ButtonText(new Rect(x, actions.y, ActionWidth, actions.height), "Pickle_Cancel".Translate())) {
      renamingPath = null;
    }

    x -= ActionWidth + ActionGap;
    GUI.enabled = problem == null;
    if (Widgets.ButtonText(new Rect(x, actions.y, ActionWidth, actions.height), "Pickle_Rename".Translate())) {
      Rename(entry, renameText.Trim());
    }

    GUI.enabled = true;
  }

  private void Rename(FixtureEntry entry, string newName) {
    if (!string.Equals(newName, entry.Name, StringComparison.Ordinal)) {
      try {
        File.Move(entry.FullPath, TargetPath(entry, newName));
      } catch (Exception ex) {
        Log.Error(ex, "pickle: renaming fixture failed");
        Messages.Message("Pickle_FixtureRenameFailed".Translate(entry.Name), MessageTypeDefOf.RejectInput, false);
      }
    }

    Refresh();
  }

  private void ConfirmDelete(FixtureEntry entry) {
    Find.WindowStack.Add(Dialog_MessageBox.CreateConfirmation(
        "Pickle_DeleteFixtureConfirm".Translate(entry.Name, entry.FullPath),
        () => Delete(entry),
        destructive: true));
  }

  private void Delete(FixtureEntry entry) {
    try {
      File.Delete(entry.FullPath);
    } catch (Exception ex) {
      Log.Error(ex, "pickle: deleting fixture failed");
      Messages.Message("Pickle_FixtureDeleteFailed".Translate(entry.Name), MessageTypeDefOf.RejectInput, false);
    }

    Refresh();
  }

  private void Refresh() {
    groups.Clear();
    headers.Clear();
    renamingPath = null;

    foreach (DiscoveredSuite suite in SuiteScanner.DiscoverSuites()) {
      List<FixtureEntry> entries = [.. FixtureCatalog
          .Read(suite.FixturesDir, suite.WritableFixturesDir)
          .Where(e => !e.IsShadowed)
          .OrderBy(e => e.Name, StringComparer.OrdinalIgnoreCase)];
      groups.Add((suite, entries));

      foreach (FixtureEntry entry in entries) {
        headers[entry.FullPath] = FixtureHeader.Read(entry.FullPath);
      }
    }
  }
}
