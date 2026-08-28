using HarmonyLib;
using Pickle.Patching;
using UnityEngine;
using Verse;

namespace Pickle.Patches.Harmony;

/// <summary>
/// Pickle's hooks expressed as Harmony patches. Registered at the lower priority,
/// so Concord wins when both libraries are active.
/// </summary>
[StaticConstructorOnStartup]
public class HarmonyBackend : IPatchBackend {
  static HarmonyBackend() {
    PatchBackends.Register(new HarmonyBackend(), PatchBackends.HarmonyPriority);
  }

  public string Name => "Harmony";

  public static void LogErrorPostfix(string text) {
    PickleHooks.AfterLogError(text);
  }

  public static void UIRootOnGUIPrefix() {
    PickleHooks.BeforeUIRootOnGUI();
  }

  public static void UIRootOnGUIPostfix() {
    PickleHooks.AfterUIRootOnGUI();
  }

  public static void ButtonTextPostfix(Rect rect, string label) {
    PickleHooks.AfterButtonText(rect, label);
  }

  // Harmony skips the original when a prefix returns false.
  public static bool AddPrefix(Window window) {
    return PickleHooks.ShouldAddWindow(window);
  }

  public void Apply() {
    HarmonyLib.Harmony harmony = new HarmonyLib.Harmony("cryptiklemur.pickle");

    harmony.Patch(
        typeof(Log).GetMethod("Error", [typeof(string)]),
        postfix: Handler(nameof(LogErrorPostfix)));

    harmony.Patch(
        typeof(UIRoot).GetMethod(nameof(UIRoot.UIRootOnGUI)),
        prefix: Handler(nameof(UIRootOnGUIPrefix)),
        postfix: Handler(nameof(UIRootOnGUIPostfix)));

    harmony.Patch(
        typeof(Widgets).GetMethod(
            nameof(Widgets.ButtonText),
            [typeof(Rect), typeof(string), typeof(bool), typeof(bool), typeof(Color), typeof(bool), typeof(TextAnchor?)]),
        postfix: Handler(nameof(ButtonTextPostfix)));

    harmony.Patch(
        typeof(WindowStack).GetMethod(nameof(WindowStack.Add)),
        prefix: Handler(nameof(AddPrefix)));
  }

  private static HarmonyMethod Handler(string name) {
    return new HarmonyMethod(typeof(HarmonyBackend).GetMethod(name));
  }
}
