using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Patching;

/// <summary>
/// Picks one backend and applies it. [StaticConstructorOnStartup] order is undefined, so
/// nothing applies until all have registered. Highest priority wins, so Concord beats Harmony.
/// </summary>
[StaticConstructorOnStartup]
public static class PatchBackends {
  /// <summary>The priority Concord backends register with, high enough to always beat Harmony.</summary>
  public const int ConcordPriority = 100;

  /// <summary>The priority Harmony backends register with.</summary>
  public const int HarmonyPriority = 0;

  private static readonly List<(IPatchBackend Backend, int Priority)> Registered = new();

  private static bool applied;
  private static bool appliedEarly;

  static PatchBackends() {
    // Runs after every static constructor, so the registry is complete by now.
    LongEventHandler.ExecuteWhenFinished(ApplyBest);
  }

  /// <summary>Adds a backend to the pool <see cref="ApplyBest"/> picks from. Backends call this from their own static constructors.</summary>
  /// <param name="backend">The backend to register.</param>
  /// <param name="priority">Its priority; the highest registered value wins.</param>
  public static void Register(IPatchBackend backend, int priority) {
    Registered.Add((backend, priority));
  }

  /// <summary>Finds and applies the highest priority backend by scanning loaded assemblies, before any backend has
  /// registered itself. Falls to the next backend if one throws.</summary>
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
      Log.WarnTo(PickleLog.Channel, "no patching backend found yet; patch attribution is off for this run.");
      return;
    }

    // A backend that throws must not take the run down with it when another one is
    // loaded and working, so each is tried in turn.
    foreach ((IPatchBackend backend, int _) in found.OrderByDescending(r => r.Priority)) {
      try {
        backend.ApplyEarly();
        PatchAttribution.Arm();
        Log.InfoTo(PickleLog.Channel, "early hooks applied via {Backend}", [backend.Name]);
        return;
      } catch (Exception ex) {
        Log.ErrorTo(PickleLog.Channel, ex, $"{backend.Name} backend failed to apply early patches");
      }
    }

    Log.ErrorTo(PickleLog.Channel, "every patching backend failed to apply early hooks; attribution is off.");
  }

  /// <summary>Applies the highest priority registered backend, falling to the next one if it throws. Runs once, after every static constructor has fired.</summary>
  public static void ApplyBest() {
    if (applied) {
      return;
    }

    applied = true;

    if (Registered.Count == 0) {
      Log.ErrorTo(PickleLog.Channel, "no patching backend loaded; Pickle needs Harmony or Concord active.");
      MissingBackendNotice.ShowIfDevMode();
      return;
    }

    // A backend that throws hands over to the next one rather than leaving the game
    // unpatched while a working library sits right there.
    foreach ((IPatchBackend backend, int priority) in Registered.OrderByDescending(r => r.Priority)) {
      try {
        backend.Apply();
      } catch (Exception ex) {
        Log.ErrorTo(PickleLog.Channel, ex, $"{backend.Name} backend failed to apply patches");
        continue;
      }

      string others = string.Join(", ", Registered.Where(r => r.Backend != backend).Select(r => r.Backend.Name));
      if (others.Length == 0) {
        Log.InfoTo(PickleLog.Channel, "patched via {Backend}", [backend.Name]);
      } else {
        Log.InfoTo(PickleLog.Channel,
            "patched via {Backend} (priority {Priority}); idle: {Others}",
            [backend.Name, priority, others]);
      }

      return;
    }

    Log.ErrorTo(PickleLog.Channel, "every patching backend failed to apply; Pickle cannot run steps.");
    MissingBackendNotice.ShowIfDevMode();
  }

  private static void CollectBackends(Assembly assembly, List<(IPatchBackend Backend, int Priority)> into) {
    Type[] types;
    try {
      types = assembly.GetTypes();
    } catch (ReflectionTypeLoadException ex) {
      types = [.. ex.Types.Where(t => t != null)!];
    } catch (Exception) {
      // A dynamic or half-loaded assembly can throw something other than
      // ReflectionTypeLoadException, and this runs from PickleMod's constructor.
      return;
    }

    foreach (Type type in types) {
      if (!IsBackend(type)) {
        continue;
      }

      try {
        into.Add(((IPatchBackend)Activator.CreateInstance(type)!, PriorityOf(type)));
      } catch (Exception ex) {
        Log.WarnTo(PickleLog.Channel, ex, $"could not create backend {type.Name}");
      }
    }
  }

  // IsAssignableFrom walks the interface map, so a type salvaged out of a
  // ReflectionTypeLoadException throws again here when its interface is the missing one.
  // Quiet, because one broken mod would otherwise log a line per type it ships.
  private static bool IsBackend(Type type) {
    try {
      return !type.IsAbstract && typeof(IPatchBackend).IsAssignableFrom(type);
    } catch (Exception) {
      return false;
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
