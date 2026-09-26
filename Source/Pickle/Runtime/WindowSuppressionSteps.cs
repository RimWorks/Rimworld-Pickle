using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Opens windows so a scenario can watch what suppression does to them. The two openers assert
/// nothing themselves: under suppression the window is meant not to arrive, and it is the
/// scenario that says which outcome it expects, through the vanilla window steps.
/// </summary>
// Windows are added straight to the stack rather than through a tab or a dialog of the game's
// own: a main tab stays the current tab even when its window is dropped, so reopening it later
// would do nothing and the scenario would read a stale result as a pass.
[PickleSteps]
public class WindowSuppressionSteps {
  // One instance of a steps class serves one scenario, so these hold that scenario's windows
  // and nothing leaks into the next one.
  private Dialog_MessageBox? foreign;
  private TagClickTestWindow? own;

  /// <summary>Opens a window belonging to the game, which suppression is meant to drop.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <returns>A task that completes once the stack has had frames to take the window.</returns>
  [When("a window Pickle does not own opens")]
  public async Task OpenForeignWindow(PickleContext ctx) {
    foreign = new Dialog_MessageBox("pickle window suppression test");
    Find.WindowStack.Add(foreign);
    await ctx.WaitFrames(2);
  }

  /// <summary>Opens a window of Pickle's own, which suppression must never drop.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <returns>A task that completes once the stack has had frames to take the window.</returns>
  [When("a window Pickle owns opens")]
  public async Task OpenOwnWindow(PickleContext ctx) {
    own = new TagClickTestWindow();
    Find.WindowStack.Add(own);
    await ctx.WaitFrames(2);
  }

  /// <summary>Takes both windows off the stack, whatever the scenario did with them.</summary>
  // A scenario that fails midway would otherwise leave its window sitting over the next one,
  // which is the very failure these steps exist to catch.
  [AfterScenario]
  public void CloseOpenedWindows() {
    foreach (Window window in new Window?[] { foreign, own }.OfType<Window>()) {
      Find.WindowStack.TryRemove(window, doCloseSound: false);
    }

    foreign = null;
    own = null;
  }
}
