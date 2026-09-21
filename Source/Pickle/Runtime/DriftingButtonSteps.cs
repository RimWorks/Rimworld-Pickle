using System.Linq;
using RimWorks.Pickle.Core.Steps;
using Verse;

namespace RimWorks.Pickle.Runtime;

/// <summary>Steps that open a window with a button still sliding into place, so the steps that wait for
/// a control to stand still have one to wait for. Fixtures for the runner's own features, undocumented.</summary>
[PickleSteps]
public class DriftingButtonSteps {
  /// <summary>Opens a window whose button slides for a number of frames and then stays put.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="frames">How many frames the button keeps sliding after the window opens.</param>
  [Given("a window whose button drifts for {int} frames")]
  public void WindowWithDriftingButton(PickleContext ctx, int frames) {
    DriftingButtonWindow.Clicked = false;
    Find.WindowStack.Add(new DriftingButtonWindow(frames));
  }

  /// <summary>Passes when the drifting button received a click.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [Then("the drifting button was clicked")]
  public void DriftingButtonWasClicked(PickleContext ctx) {
    ctx.Assert(DriftingButtonWindow.Clicked, "the click on the drifting button did not count");
  }

  /// <summary>Passes when the drifting button received no click, because a click sent while it slid missed it.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [Then("the drifting button was not clicked")]
  public void DriftingButtonWasNotClicked(PickleContext ctx) {
    ctx.Assert(!DriftingButtonWindow.Clicked, "the click on the drifting button counted");
  }

  /// <summary>Closes the fixture window, so a scenario that fails half way leaves nothing over the next.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [AfterScenario]
  public void CloseDriftingButtonWindow(PickleContext ctx) {
    foreach (Window window in Find.WindowStack.Windows.ToArray()) {
      if (window is DriftingButtonWindow) {
        Find.WindowStack.TryRemove(window, doCloseSound: false);
      }
    }
  }
}
