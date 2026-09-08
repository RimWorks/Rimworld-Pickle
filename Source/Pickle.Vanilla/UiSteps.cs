using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.UI;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Clicking, keys, tabs, windows and selection, the steps every scenario builds on.</summary>
[PickleSteps]
public class UiSteps {
  private const string Nothing = "(none)";

  /// <summary>Returns to the main menu, waiting out any running long event first.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
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

  /// <summary>Clicks the UI element registered under a tag.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="tag">The tag the target element was registered under.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I click {string}")]
  public async Task ClickTag(PickleContext ctx, string tag) {
    await ctx.Click(tag);
  }

  /// <summary>Clicks a button by its label.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="label">The button's label.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I click button {string}")]
  public async Task ClickButton(PickleContext ctx, string label) {
    await ctx.Click($"btn:{label}");
  }

  /// <summary>Presses a key.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="key">The key to press.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I press key {string}")]
  public async Task PressKey(PickleContext ctx, string key) {
    await ctx.PressKey(key);
  }

  /// <summary>Hovers the UI element registered under a tag.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="tag">The tag the target element was registered under.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I hover {string}")]
  public async Task HoverTag(PickleContext ctx, string tag) {
    await ctx.Hover(tag);
  }

  /// <summary>Switches the main tabs root to the named tab.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="tabName">The tab's def name or label.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
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

  /// <summary>Asserts a window with the given type name is open.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="windowName">The window's type name.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // A page transition takes more than the one frame this used to wait, and how many more
  // depends on the machine, so it waits on the window rather than on a frame count.
  [Then("window {string} is open")]
  public async Task AssertWindowOpen(PickleContext ctx, string windowName) {
    await ctx.AssertEventually(
        () => IsWindowOpen(windowName),
        () => $"window '{windowName}' should be open; open windows: {DescribeOpenWindows()}");
  }

  /// <summary>Asserts a window with the given type name is not open.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="windowName">The window's type name.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [Then("window {string} is closed")]
  public async Task AssertWindowClosed(PickleContext ctx, string windowName) {
    await ctx.AssertEventually(
        () => !IsWindowOpen(windowName),
        () => $"window '{windowName}' should be closed; open windows: {DescribeOpenWindows()}");
  }

  /// <summary>Selects a thing on the map by label, bypassing a real click.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="label">The thing's label, or a pawn's short name.</param>
  [When("I select {string}")]
  public void Select(PickleContext ctx, string label) {
    Map map = RequireMap(ctx);
    Thing thing = RequireSelectableThing(map, label);

    // Semantic selection: sets Find.Selector state directly, not a real click.
    Find.Selector.ClearSelection();
    Find.Selector.Select(thing, playSound: false);
  }

  /// <summary>Runs the gizmo with the given label on the current selection.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="label">The gizmo's label.</param>
  [When("I click gizmo {string}")]
  public void ClickGizmo(PickleContext ctx, string label) {
    Command command = RequireGizmo(label);
    command.ProcessInput(null!);
  }

  /// <summary>Closes every open window except the runner's own.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  [When("I close all dialogs")]
  public void CloseAllDialogs(PickleContext ctx) {
    List<Window> toClose = [.. Find.WindowStack.Windows.Where(w => w is not RunnerWindow)];

    foreach (Window window in toClose) {
      Find.WindowStack.TryRemove(window, doCloseSound: false);
    }
  }

  /// <summary>Asserts the inspect pane's label contains a substring.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="expectedSubstring">The substring the label should contain.</param>
  [Then("the inspect pane shows {string}")]
  public void AssertInspectPaneShows(PickleContext ctx, string expectedSubstring) {
    Thing? selected = Find.Selector.SingleSelectedThing;
    string actualLabel = selected?.LabelCap ?? "(nothing selected)";
    ctx.Assert(
        actualLabel.IndexOf(expectedSubstring, StringComparison.OrdinalIgnoreCase) >= 0,
        $"inspect pane should show '{expectedSubstring}'; actually showing: {actualLabel}");
  }

  /// <summary>Asserts no error has been logged since <see cref="LogWatch"/> was armed.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  [Then("no errors were logged")]
  public void AssertNoErrorsLogged(PickleContext ctx) {
    ctx.Assert(
        LogWatch.ErrorCount == 0,
        $"expected no errors logged; got {LogWatch.ErrorCount}: {string.Join(" | ", LogWatch.ErrorsSinceArmed)}");
  }

  /// <summary>Asserts at least one warning matching a substring was logged.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="substring">The substring to look for, case insensitive.</param>
  [Then("a warning matching {string} was logged")]
  public void AssertWarningLogged(PickleContext ctx, string substring) {
    List<string> matches = WarningsMatching(substring);
    ctx.Assert(
        matches.Count > 0,
        matches.Count > 0 ? null : $"expected a warning matching '{substring}'; logged: {DescribeWarnings()}");
  }

  /// <summary>Asserts no warning matching a substring was logged. Vanilla warns constantly,
  /// so this is scoped to a substring an author names rather than a blanket check.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="substring">The substring that must not appear, case insensitive.</param>
  [Then("no warning matching {string} was logged")]
  public void AssertNoWarningLogged(PickleContext ctx, string substring) {
    RequireWarningsNotDropped(ctx);
    List<string> matches = WarningsMatching(substring);
    ctx.Assert(
        matches.Count == 0,
        $"expected no warning matching '{substring}'; got {matches.Count}: {string.Join(" | ", matches)}");
  }

  /// <summary>Asserts an exact number of warnings matching a substring were logged.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="expectedCount">The exact count of matching warnings expected.</param>
  /// <param name="substring">The substring to match, case insensitive.</param>
  [Then("{int} warnings matching {string} were logged")]
  public void AssertWarningCount(PickleContext ctx, int expectedCount, string substring) {
    List<string> matches = WarningsMatching(substring);
    ctx.Assert(
        matches.Count == expectedCount,
        $"expected {expectedCount} warning(s) matching '{substring}'; got {matches.Count}: {string.Join(" | ", matches)}");
  }

  /// <summary>Asserts no warning attributed to a mod was logged.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="modName">The mod's display name, as RimLogging attributes it.</param>
  [Then("no warnings from mod {string}")]
  public void AssertNoWarningsFromMod(PickleContext ctx, string modName) {
    ctx.Require(
        ModLookup.IsLoaded(modName),
        $"mod '{modName}' is not loaded. loaded mods: {ModLookup.DescribeLoadOrder()}; " +
        $"warnings seen from: {DescribeObservedMods()}");
    RequireWarningsNotDropped(ctx);

    List<string> matches = [.. LogWatch.WarningsSinceArmed
        .Where(w => string.Equals(w.Mod, modName, StringComparison.OrdinalIgnoreCase))
        .Select(w => w.Message)];
    ctx.Assert(
        matches.Count == 0,
        $"expected no warnings from mod '{modName}'; got {matches.Count}: {string.Join(" | ", matches)}");
  }

  /// <summary>Captures the current frame to a file and attaches it to the report.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="name">The name to give the attachment.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
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

    return names.Count == 0 ? Nothing : string.Join(", ", names);
  }

  private static List<string> WarningsMatching(string substring) {
    return [.. LogWatch.WarningsSinceArmed
        .Where(w => w.Message.IndexOf(substring, StringComparison.OrdinalIgnoreCase) >= 0)
        .Select(w => w.Message)];
  }

  private static string DescribeWarnings() {
    return LogWatch.WarningsSinceArmed.Count == 0
        ? Nothing
        : string.Join(" | ", LogWatch.WarningsSinceArmed.Select(w => w.Message));
  }

  private static string DescribeObservedMods() {
    List<string> mods = [.. LogWatch.WarningsSinceArmed
        .Select(w => w.Mod)
        .OfType<string>()
        .Distinct(StringComparer.OrdinalIgnoreCase)
        .OrderBy(m => m, StringComparer.OrdinalIgnoreCase)];

    return mods.Count == 0 ? Nothing : string.Join(", ", mods);
  }

  // A "no warning" step cannot tell an empty buffer from a wiped one once the 50-slot ring
  // has evicted entries recorded after Arm: the one it was looking for could be the one gone.
  private static void RequireWarningsNotDropped(PickleContext ctx) {
    long dropped = LogWatch.WarningsDroppedSinceArmed;
    ctx.Require(
        dropped == 0,
        $"{dropped} warning(s) rolled out of the buffer since this scenario armed; " +
        "cannot tell an absent warning from a dropped one");
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

    return labels.Count == 0 ? Nothing : string.Join(", ", labels);
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

    return labels.Count == 0 ? Nothing : string.Join(", ", labels);
  }

  private static Map RequireMap(PickleContext ctx) {
    Map? map = Find.CurrentMap;
    ctx.Require(map != null, "no current map is loaded; load a save first with 'the save ... is loaded'");
    return map!;
  }
}
