using System.Reflection;
using Concord;
using Pickle.Patching;
using UnityEngine;
using Verse;

namespace Pickle.Patches.Concord;

/// <summary>
/// Pickle's hooks expressed as Concord injections. Registered above Harmony, so
/// Concord is used whenever it is available.
/// </summary>
[StaticConstructorOnStartup]
public class ConcordBackend : IPatchBackend {
  static ConcordBackend() {
    PatchBackends.Register(new ConcordBackend(), PatchBackends.ConcordPriority);
  }

  public string Name => "Concord";

  public static void AfterLogError(string text) {
    PickleHooks.AfterLogError(text);
  }

  public static void BeforeUIRootOnGUI() {
    PickleHooks.BeforeUIRootOnGUI();
  }

  public static void AfterUIRootOnGUI() {
    PickleHooks.AfterUIRootOnGUI();
  }

  public static void AfterButtonText(Rect rect, string label) {
    PickleHooks.AfterButtonText(rect, label);
  }

  // Concord skips the original when a head injection returns Control.Cancel.
  public static Control BeforeWindowAdd(Window window) {
    return PickleHooks.ShouldAddWindow(window) ? Control.Continue : Control.Cancel;
  }

  public void Apply() {
    Patcher.Patch(
        typeof(Log).GetMethod("Error", [typeof(string)]),
        Injection(nameof(AfterLogError)),
        At.Tail);

    Patcher.Patch(
        typeof(UIRoot).GetMethod(nameof(UIRoot.UIRootOnGUI)),
        Injection(nameof(BeforeUIRootOnGUI)),
        At.Head);

    Patcher.Patch(
        typeof(UIRoot).GetMethod(nameof(UIRoot.UIRootOnGUI)),
        Injection(nameof(AfterUIRootOnGUI)),
        At.Tail);

    Patcher.Patch(
        typeof(Widgets).GetMethod(
            nameof(Widgets.ButtonText),
            [typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(Color), typeof(bool), typeof(TextAnchor?)]),
        Injection(nameof(AfterButtonText)),
        At.Tail);

    Patcher.Patch(
        typeof(WindowStack).GetMethod(nameof(WindowStack.Add)),
        Injection(nameof(BeforeWindowAdd)),
        At.Head);
  }

  private static MethodBase Injection(string name) {
    return typeof(ConcordBackend).GetMethod(name)!;
  }
}
