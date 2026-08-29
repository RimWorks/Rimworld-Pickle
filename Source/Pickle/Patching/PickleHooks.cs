using System;
using Pickle.Autorun;
using Pickle.Input;
using Pickle.UI;
using UnityEngine;
using Verse;

namespace Pickle.Patching;

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

  public static void AfterLogError(string text) {
    LogWatch.RecordError(text);
  }

  public static void BeforeUIRootOnGUI() {
    EventSynth.BeforeUIRootOnGUI();
  }

  public static void AfterUIRootOnGUI() {
    TagOverlay.DrawOverlay();
    DuringUIRootOnGUI?.Invoke();
  }

  public static void AfterButtonText(Rect rect, string label) {
    WidgetCapture.AfterButtonText(rect, label);
  }

  /// <summary>
  /// False drops the window. Autorun suppresses windows opened while a fixture loads.
  /// </summary>
  public static bool ShouldAddWindow(Window window) {
    return AutorunDialogSuppression.ShouldAdd(window);
  }

  public static void BeforeApplyPatches(System.Xml.XmlDocument xmlDoc) {
    PatchAttribution.BeforeApplyPatches(xmlDoc);
  }

  public static void BeforeClearCachedPatches() {
    PatchAttribution.BeforeClearCachedPatches();
  }
}
