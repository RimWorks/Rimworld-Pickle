using System;
using RimWorks.Pickle.Autorun;
using RimWorks.Pickle.Input;
using RimWorks.Pickle.UI;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Patching;

/// <summary>
/// What Pickle wants at each patch site, with no patching library in the signatures.
/// Backends differ mainly in how a hook says "skip the original".
/// </summary>
public static class PickleHooks {
  /// <summary>
  /// Extra work to run inside the patched OnGUI. Dev smokes use this instead of applying
  /// a second patch, which would need a patching library in the core.
  /// </summary>
  public static Action? DuringUIRootOnGUI { get; set; }

  /// <summary>Runs before <c>UIRoot.OnGUI</c>, letting <see cref="EventSynth"/> deliver a synthesized key event while a native OnGUI is on the stack.</summary>
  public static void BeforeUIRootOnGUI() {
    EventSynth.BeforeUIRootOnGUI();
  }

  /// <summary>Runs after <c>UIRoot.OnGUI</c>, drawing the tag overlay and any dev smoke work queued in <see cref="DuringUIRootOnGUI"/>.</summary>
  public static void AfterUIRootOnGUI() {
    TagOverlay.DrawOverlay();
    DuringUIRootOnGUI?.Invoke();
  }

  /// <summary>Runs after the main menu draws its controls, adding the runner button.</summary>
  /// <param name="rect">The rect the main menu controls occupy.</param>
  public static void AfterMainMenuControls(Rect rect) {
    MainMenuRunnerButton.Draw(rect);
  }

  /// <summary>Runs after a button draws its text, letting <see cref="WidgetCapture"/> record it for tag matching.</summary>
  /// <param name="rect">The rect the button occupies.</param>
  /// <param name="label">The button's text.</param>
  public static void AfterButtonText(Rect rect, string label) {
    WidgetCapture.AfterButtonText(rect, label);
  }

  /// <summary>
  /// False drops the window. Autorun suppresses windows opened while a fixture loads.
  /// </summary>
  /// <param name="window">The window about to be added.</param>
  /// <returns><c>false</c> to drop the window instead of adding it.</returns>
  public static bool ShouldAddWindow(Window window) {
    return AutorunDialogSuppression.ShouldAdd(window);
  }

  /// <summary>Runs before RimWorld applies XML patches, recording which mod owns each patch operation.</summary>
  /// <param name="xmlDoc">The unified XML document patches are about to apply against.</param>
  public static void BeforeApplyPatches(System.Xml.XmlDocument xmlDoc) {
    PatchAttribution.BeforeApplyPatches(xmlDoc);
  }

  /// <summary>Runs before RimWorld clears its cached patch operations, so attribution can read them first.</summary>
  public static void BeforeClearCachedPatches() {
    PatchAttribution.BeforeClearCachedPatches();
  }
}
