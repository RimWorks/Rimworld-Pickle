using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using HarmonyLib;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>The research window: opening a tab by def name and reading what it lists.</summary>
// The window draws its tabs as TabRecords through TabDrawer, which records no button tag, so
// `I click button` cannot reach one. These steps run the tab record's own click action instead.
[PickleSteps]
public class ResearchSteps {
  /// <summary>Opens the research window and selects a tab by def name, whatever language the game runs in.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="tabDefName">The <c>ResearchTabDef</c>'s def name, such as <c>Main</c>.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I open the research tab {string}")]
  public async Task OpenResearchTab(PickleContext ctx, string tabDefName) {
    ResearchTabDef tab = DefLookup.Require<ResearchTabDef>(tabDefName);

    Find.MainTabsRoot.SetCurrentTab(MainButtonDefOf.Research, playSound: false);
    await ctx.WaitUntil(() => Records(Window()).Count > 0, 10f);

    MainTabWindow_Research? window = Window();
    ctx.Require(window != null, "the research window did not open");

    TabRecord? record = RecordOf(window!, tab);
    ctx.Require(
        record != null,
        $"the research window built no tab record for '{tabDefName}'. it built: {DescribeRecords(window!)}");

    // What a click on the tab runs: it sets the window's current tab and its selected project.
    record!.clickedAction();
    await ctx.WaitFrames(2);
  }

  /// <summary>Asserts the research window shows a tab, and that the tab would draw its projects.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="tabDefName">The <c>ResearchTabDef</c>'s def name.</param>
  [Then("the research window is on the tab {string}")]
  public void AssertOnResearchTab(PickleContext ctx, string tabDefName) {
    MainTabWindow_Research? window = Window();
    ctx.Require(window != null, "the research window is not open");

    ResearchTabDef? current = window!.CurTab;
    ctx.Assert(
        current != null && current.defName == tabDefName,
        $"the research window is on '{current?.defName ?? "no tab"}', not '{tabDefName}'");

    // A tab whose info is not visible draws its "not discovered" text in place of its projects.
    ctx.Assert(
        current != null && Find.ResearchManager.TabInfoVisible(current),
        $"'{tabDefName}' is selected but its info is not visible, so the window would not list its projects");
  }

  /// <summary>Asserts the research window lists a project on the tab it is on.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="projectDefName">The <c>ResearchProjectDef</c>'s def name.</param>
  [Then("the research window lists the project {string}")]
  public void AssertListsResearchProject(PickleContext ctx, string projectDefName) {
    MainTabWindow_Research? window = Window();
    ctx.Require(window != null, "the research window is not open");
    ResearchTabDef? current = window!.CurTab;
    ctx.Require(current != null, "the research window has no current tab");

    ResearchProjectDef project = DefLookup.Require<ResearchProjectDef>(projectDefName);

    // The list the window draws from: the visible projects whose tab is the selected one.
    List<ResearchProjectDef> listed = [.. window.VisibleResearchProjects.Where(p => p.tab == current)];
    bool isListed = listed.Contains(project);
    ctx.Assert(
        isListed,
        isListed
            ? null
            : $"the window does not list '{projectDefName}' on '{current!.defName}'. " +
              $"it lists: {string.Join(", ", listed.Select(p => p.defName))}");
  }

  private static MainTabWindow_Research? Window() {
    return Find.WindowStack?.WindowOfType<MainTabWindow_Research>();
  }

  // The window keeps its tab records in a private list, filled when it opens.
  private static List<TabRecord> Records(MainTabWindow_Research? window) {
    List<TabRecord> records = [];
    if (window == null) {
      return records;
    }

    FieldInfo field = AccessTools.Field(typeof(MainTabWindow_Research), "tabs")
        ?? throw new InvalidOperationException("MainTabWindow_Research.tabs no longer exists: update the step");

    if (field.GetValue(window) is IEnumerable list) {
      records.AddRange(list.Cast<TabRecord>());
    }

    return records;
  }

  // The record is a private nested class whose def is a public field, so it is read by reflection.
  private static ResearchTabDef DefOf(TabRecord record) {
    FieldInfo field = AccessTools.Field(record.GetType(), "def")
        ?? throw new InvalidOperationException($"{record.GetType().Name}.def no longer exists: update the step");

    return (ResearchTabDef)field.GetValue(record);
  }

  private static TabRecord? RecordOf(MainTabWindow_Research window, ResearchTabDef tab) {
    return Records(window).FirstOrDefault(r => DefOf(r) == tab);
  }

  private static string DescribeRecords(MainTabWindow_Research window) {
    return string.Join(", ", Records(window).Select(r => DefOf(r).defName));
  }
}
