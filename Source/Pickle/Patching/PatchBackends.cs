using System;
using System.Collections.Generic;
using System.Linq;
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

  static PatchBackends() {
    // Runs after every static constructor, so the registry is complete by now.
    LongEventHandler.ExecuteWhenFinished(ApplyBest);
  }

  public static void Register(IPatchBackend backend, int priority) {
    Registered.Add((backend, priority));
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
}
