using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;

namespace Pickle.Patching;

/// <summary>
/// Picks one backend and applies it. [StaticConstructorOnStartup] order is undefined, so
/// nothing applies until all have registered. Highest priority wins, so Concord beats Harmony.
/// </summary>
[StaticConstructorOnStartup]
public static class PatchBackends {
  public const int ConcordPriority = 100;
  public const int HarmonyPriority = 0;

  private static readonly List<(IPatchBackend Backend, int Priority)> Registered = new();

  private static bool applied;
  private static bool appliedEarly;

  static PatchBackends() {
    // Runs after every static constructor, so the registry is complete by now.
    LongEventHandler.ExecuteWhenFinished(ApplyBest);
  }

  public static void Register(IPatchBackend backend, int priority) {
    Registered.Add((backend, priority));
  }

  // Runs from PickleMod's constructor, which is the last point before RimWorld applies XML
  // patches. No static constructor has fired yet, so the backends are found by scanning.
  public static void ApplyEarliest() {
    if (appliedEarly) {
      return;
    }

    appliedEarly = true;

    List<(IPatchBackend Backend, int Priority)> found = [];
    foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies()) {
      CollectBackends(assembly, found);
    }

    if (found.Count == 0) {
      Log.Warning("pickle: no patching backend found yet; patch attribution is off for this run.");
      return;
    }

    IPatchBackend backend = found.OrderByDescending(r => r.Priority).First().Backend;

    try {
      backend.ApplyEarly();
      PatchAttribution.Arm();
      Log.Message($"pickle: early hooks applied via {backend.Name}");
    } catch (Exception ex) {
      Log.Error($"pickle: {backend.Name} backend failed to apply early patches: {ex}");
    }
  }

  public static void ApplyBest() {
    if (applied) {
      return;
    }

    applied = true;

    if (Registered.Count == 0) {
      Log.Error("pickle: no patching backend loaded; Pickle needs Harmony or Concord active.");
      MissingBackendNotice.ShowIfDevMode();
      return;
    }

    (IPatchBackend backend, int priority) = Registered.OrderByDescending(r => r.Priority).First();

    try {
      backend.Apply();
    } catch (Exception ex) {
      Log.Error($"pickle: {backend.Name} backend failed to apply patches: {ex}");
      return;
    }

    string others = string.Join(", ", Registered.Where(r => r.Backend != backend).Select(r => r.Backend.Name));
    Log.Message(others.Length == 0
        ? $"pickle: patched via {backend.Name}"
        : $"pickle: patched via {backend.Name} (priority {priority}); idle: {others}");
  }

  private static void CollectBackends(Assembly assembly, List<(IPatchBackend Backend, int Priority)> into) {
    Type[] types;
    try {
      types = assembly.GetTypes();
    } catch (ReflectionTypeLoadException ex) {
      types = [.. ex.Types.Where(t => t != null)!];
    }

    foreach (Type type in types) {
      if (type.IsAbstract || !typeof(IPatchBackend).IsAssignableFrom(type)) {
        continue;
      }

      try {
        into.Add(((IPatchBackend)Activator.CreateInstance(type)!, PriorityOf(type)));
      } catch (Exception ex) {
        Log.Warning($"pickle: could not create backend {type.Name}: {ex.Message}");
      }
    }
  }

  // The backend types name themselves in their own static constructors, which have not run
  // yet, so priority comes from the type name instead.
  private static int PriorityOf(Type type) {
    return type.Name.IndexOf("Concord", StringComparison.OrdinalIgnoreCase) >= 0
        ? ConcordPriority
        : HarmonyPriority;
  }
}
