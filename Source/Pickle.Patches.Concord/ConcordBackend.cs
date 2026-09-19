using System;
using System.Reflection;
using System.Xml;
using Concord;
using RimWorks.Pickle.Patching;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Patches.Concord;

/// <summary>
/// Pickle's hooks expressed as Concord injections. Registered above Harmony, so
/// Concord is used whenever it is available.
/// </summary>
[StaticConstructorOnStartup]
public class ConcordBackend : IPatchBackend {
  private const string BackendName = "Concord";

  static ConcordBackend() {
    PatchBackends.Register(new ConcordBackend(), PatchBackends.ConcordPriority);
  }

  /// <inheritdoc/>
  public string Name => BackendName;

  /// <summary>Head injection on <see cref="UIRoot.UIRootOnGUI"/> that runs Pickle's per-frame work before the game draws.</summary>
  public static void BeforeUIRootOnGUI() {
    PickleHooks.BeforeUIRootOnGUI();
  }

  /// <summary>Tail injection on <see cref="UIRoot.UIRootOnGUI"/> that runs Pickle's per-frame work after the game draws.</summary>
  public static void AfterUIRootOnGUI() {
    PickleHooks.AfterUIRootOnGUI();
  }

  /// <summary>Tail injection on <see cref="MainMenuDrawer.DoMainMenuControls"/> that draws Pickle's runner button.</summary>
  /// <param name="rect">The rect the main menu just laid its controls out in.</param>
  public static void AfterMainMenuControls(Rect rect) {
    PickleHooks.AfterMainMenuControls(rect);
  }

  /// <summary>Tail injection on <c>Widgets.ButtonText</c> that lets Pickle capture the button for a click step.</summary>
  /// <param name="rect">The rect the button was drawn in.</param>
  /// <param name="label">The button's label text.</param>
  public static void AfterButtonText(Rect rect, string label) {
    PickleHooks.AfterButtonText(rect, label);
  }

  /// <summary>Head injection on <see cref="WindowStack.Add"/> that can drop a window autorun wants suppressed.</summary>
  /// <param name="window">The window about to be added.</param>
  /// <returns><see cref="Control.Cancel"/> to skip adding the window, <see cref="Control.Continue"/> to let it through.</returns>
  // Concord skips the original when a head injection returns Control.Cancel.
  public static Control BeforeWindowAdd(Window window) {
    return PickleHooks.ShouldAddWindow(window) ? Control.Continue : Control.Cancel;
  }

  /// <summary>Head injection on <see cref="LoadedModManager.ApplyPatches"/> that records which mod owns each patch.</summary>
  /// <param name="xmlDoc">The XML document about to be patched.</param>
  public static void BeforeApplyPatches(XmlDocument xmlDoc) {
    PickleHooks.BeforeApplyPatches(xmlDoc);
  }

  /// <summary>Head injection on <see cref="LoadedModManager.ClearCachedPatches"/> that clears Pickle's patch attribution cache alongside it.</summary>
  public static void BeforeClearCachedPatches() {
    PickleHooks.BeforeClearCachedPatches();
  }

  /// <summary>Tail injection on <see cref="PatchProbe.Target"/> that proves Concord runs what it accepts.</summary>
  public static void AfterProbeTarget() {
    PatchProbe.Record(BackendName);
  }

  /// <inheritdoc/>
  // Concord publishes its own readiness: RimWorldAdapter.Ready turns true only once wiring
  // finished, which is a direct answer where the probe is an inference. Adapters before v1.6.2
  // have no such property, and there the probe is the only signal.
  public bool Probe() {
    if (AdapterReady() == false) {
      return false;
    }

    Patcher.Patch(
        typeof(PatchProbe).GetMethod(nameof(PatchProbe.Target)),
        Injection(nameof(AfterProbeTarget)),
        At.Tail);

    return PatchProbe.Fired(BackendName);
  }

  /// <inheritdoc/>
  public void ApplyEarly() {
    Patcher.Patch(
        typeof(LoadedModManager).GetMethod(nameof(LoadedModManager.ApplyPatches)),
        Injection(nameof(BeforeApplyPatches)),
        At.Head);

    Patcher.Patch(
        typeof(LoadedModManager).GetMethod(nameof(LoadedModManager.ClearCachedPatches)),
        Injection(nameof(BeforeClearCachedPatches)),
        At.Head);
  }

  /// <inheritdoc/>
  public void Apply() {
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

    Patcher.Patch(
        typeof(MainMenuDrawer).GetMethod(nameof(MainMenuDrawer.DoMainMenuControls)),
        Injection(nameof(AfterMainMenuControls)),
        At.Tail);
  }

  /// <summary>Concord's own readiness flag, or null when the loaded adapter has none.</summary>
  // By reflection: the backend compiles against the Concord runtime, while the flag lives in the
  // RimWorld adapter, which ships with the mod rather than with the package.
  private static bool? AdapterReady() {
    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
      Type? adapter;
      try {
        adapter = assembly.GetType("Concord.RimWorld.RimWorldAdapter", throwOnError: false);
      } catch (Exception) {
        continue;
      }

      if (adapter?.GetProperty("Ready", BindingFlags.Public | BindingFlags.Static) is not { } ready
          || ready.PropertyType != typeof(bool)) {
        continue;
      }

      try {
        return (bool)ready.GetValue(null);
      } catch (Exception) {
        return null;
      }
    }

    return null;
  }

  private static MethodBase Injection(string name) {
    return typeof(ConcordBackend).GetMethod(name)!;
  }
}
