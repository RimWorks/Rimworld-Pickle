using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>Base for every window Pickle puts on the stack.</summary>
public abstract class PickleWindow : Window {
  /// <inheritdoc/>
  // UIRoot_Entry.ShouldDoMainMenu hides the menu behind any non-debug Dialog-layer window
  public override bool IsDebug => true;
}
