using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.UI;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

[PickleSteps]
public class UiSteps {
  // Waits out any long event before tearing the world down: a world-renderer layer that
  // regenerates across frames reads freed tile arrays afterwards, which is a signal 11.
  [Given("the main menu is open")]
  public async Task MainMenuIsOpen(PickleContext ctx) {
    await ctx.WaitUntil(() => !LongEventHandler.AnyEventNowOrWaiting, 60f);

    if (Current.ProgramState != ProgramState.Entry) {
      GenScene.GoToMainMenu();
    }

    await ctx.WaitUntil(
        () => Current.ProgramState == ProgramState.Entry
            && Find.UIRoot is UIRoot_Entry
            && !LongEventHandler.AnyEventNowOrWaiting,
        60f);

    // The menu only draws when nothing dialog-layer covers it, so a page left open by an
    // earlier scenario would make every click here miss.
    CloseAllDialogs(ctx);
    await ctx.WaitFrames(2);
  }

  [When("I click {string}")]
  public async Task ClickTag(PickleContext ctx, string tag) {
    await ctx.Click(tag);
  }

  [When("I click button {string}")]
  public async Task ClickButton(PickleContext ctx, string label) {
    await ctx.Click($"btn:{label}");
  }

  [When("I press key {string}")]
  public async Task PressKey(PickleContext ctx, string key) {
    await ctx.PressKey(key);
  }

  [When("I hover {string}")]
  public async Task HoverTag(PickleContext ctx, string tag) {
    await ctx.Hover(tag);
  }

  [When("I open the {string} tab")]
  public async Task OpenTab(PickleContext ctx, string tabName) {
    MainButtonDef? tab = FindTab(tabName);
    if (tab == null) {
      throw new InvalidOperationException(
          $"no tab matches '{tabName}'. available tabs: {DescribeTabs()}");
    }

    Find.MainTabsRoot.SetCurrentTab(tab, playSound: false);
    await ctx.WaitFrames(2);
  }

  [Then("window {string} is open")]
  public async Task AssertWindowOpen(PickleContext ctx, string windowName) {
    await ctx.WaitFrames(1);
    ctx.Assert(
        IsWindowOpen(windowName),
        $"window '{windowName}' should be open; open windows: {DescribeOpenWindows()}");
  }

  [Then("window {string} is closed")]
  public async Task AssertWindowClosed(PickleContext ctx, string windowName) {
    await ctx.WaitFrames(1);
    ctx.Assert(
        !IsWindowOpen(windowName),
        $"window '{windowName}' should be closed; open windows: {DescribeOpenWindows()}");
  }

  [When("I select {string}")]
  public void Select(PickleContext ctx, string label) {
    Map map = RequireMap(ctx);
    Thing thing = RequireSelectableThing(map, label);

    // Semantic selection: sets Find.Selector state directly, not a real click.
    Find.Selector.ClearSelection();
    Find.Selector.Select(thing, playSound: false);
  }

  [When("I click gizmo {string}")]
  public void ClickGizmo(PickleContext ctx, string label) {
    Command command = RequireGizmo(label);
    command.ProcessInput(null!);
  }

  [When("I close all dialogs")]
  public void CloseAllDialogs(PickleContext ctx) {
    List<Window> toClose = [.. Find.WindowStack.Windows.Where(w => w is not RunnerWindow)];

    foreach (Window window in toClose) {
      Find.WindowStack.TryRemove(window, doCloseSound: false);
    }
  }

  [Then("the inspect pane shows {string}")]
  public void AssertInspectPaneShows(PickleContext ctx, string expectedSubstring) {
    Thing? selected = Find.Selector.SingleSelectedThing;
    string actualLabel = selected?.LabelCap ?? "(nothing selected)";
    ctx.Assert(
        actualLabel.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) >= 0,
        $"inspect pane should show '{expectedSubstring}'; actually showing: {actualLabel}");
  }

  [Then("no errors were logged")]
  public void AssertNoErrorsLogged(PickleContext ctx) {
    ctx.Assert(
        LogWatch.ErrorCount == 0,
        $"expected no errors logged; got {LogWatch.ErrorCount}: {string.Join(" | ", LogWatch.ErrorsSinceArmed)}");
  }

  [When("I take a screenshot {string}")]
  public async Task TakeScreenshot(PickleContext ctx, string name) {
    // PickleContext exposes no feature/scenario name, so "manual" plus the given
    // name is the best available stand-in for a stable, traceable file name.
    string path = ScreenshotCapture.BuildScreenshotPath("manual", name, 0);
    await ScreenshotCapture.CaptureToFile(path);
    ctx.Attach(name, path);
  }

  private static MainButtonDef? FindTab(string name) {
    List<MainButtonDef> tabs = DefDatabase<MainButtonDef>.AllDefsListForReading;

    foreach (MainButtonDef tab in tabs) {
      if (string.Equals(tab.defName, name, StringComparison.OrdinalIgnoreCase)) {
        return tab;
      }
    }

    foreach (MainButtonDef tab in tabs) {
      if (tab.label != null && string.Equals(tab.label, name, StringComparison.OrdinalIgnoreCase)) {
        return tab;
      }
    }

    return null;
  }

  private static string DescribeTabs() {
    IEnumerable<string> described = DefDatabase<MainButtonDef>.AllDefsListForReading
        .Select(t => t.label != null && !string.Equals(t.label, t.defName, StringComparison.OrdinalIgnoreCase)
            ? $"{t.defName} ('{t.label}')"
            : t.defName)
        .OrderBy(n => n);

    return string.Join(", ", described);
  }

  private static bool IsWindowOpen(string windowName) {
    foreach (Window window in Find.WindowStack.Windows) {
      if (string.Equals(window.GetType().Name, windowName, StringComparison.OrdinalIgnoreCase)) {
        return true;
      }
    }

    return false;
  }

  private static string DescribeOpenWindows() {
    List<string> names = [.. Find.WindowStack.Windows
        .Select(w => w.GetType().Name)
        .OrderBy(n => n)];

    return names.Count == 0 ? "(none)" : string.Join(", ", names);
  }

  private static Thing RequireSelectableThing(Map map, string label) {
    foreach (Thing thing in map.listerThings.AllThings) {
      if (thing is Pawn pawn
          && string.Equals(pawn.Name?.ToStringShort, label, StringComparison.OrdinalIgnoreCase)) {
        return thing;
      }

      if (string.Equals(thing.LabelCap, label, StringComparison.OrdinalIgnoreCase)) {
        return thing;
      }
    }

    throw new InvalidOperationException(
        $"no selectable thing labeled '{label}' on the map. nearby candidates: {DescribeSelectableThings(map)}");
  }

  private static string DescribeSelectableThings(Map map) {
    List<string> labels = [.. map.listerThings.AllThings
        .Select(t => t is Pawn p ? p.Name?.ToStringShort ?? p.LabelCap : t.LabelCap)
        .Where(l => !string.IsNullOrEmpty(l))
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
        .Take(20)];

    return labels.Count == 0 ? "(none)" : string.Join(", ", labels);
  }

  private static Command RequireGizmo(string label) {
    List<Gizmo> gizmos = [.. Find.Selector.SelectedObjectsListForReading
        .OfType<Thing>()
        .SelectMany(t => t.GetGizmos())];

    foreach (Gizmo gizmo in gizmos) {
      if (gizmo is Command command
          && string.Equals(command.LabelCap, label, StringComparison.OrdinalIgnoreCase)) {
        return command;
      }
    }

    throw new InvalidOperationException(
        $"no gizmo labeled '{label}' on the current selection. available gizmos: {DescribeGizmos(gizmos)}");
  }

  private static string DescribeGizmos(IEnumerable<Gizmo> gizmos) {
    List<string> labels = [.. gizmos.OfType<Command>()
        .Select(c => c.LabelCap)
        .Where(l => !string.IsNullOrEmpty(l))
        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)];

    return labels.Count == 0 ? "(none)" : string.Join(", ", labels);
  }

  private static Map RequireMap(PickleContext ctx) {
    Map? map = Find.CurrentMap;
    ctx.Require(map != null, "no current map is loaded; load a save first with 'the save ... is loaded'");
    return map!;
  }
}
