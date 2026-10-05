using System;

namespace RimWorks.Pickle.Core.Steps;

/// <summary>Which assemblies are worth reflecting over to find step methods on an unregistered class.</summary>
public static class StepScanScope {
  private static readonly string[] NotMods = [
    "Assembly-CSharp",
    "UnityEngine",
    "Unity",
    "System",
    "mscorlib",
    "netstandard",
    "Mono",
    "Microsoft",
  ];

  /// <summary>Whether an assembly could hold a mod's step class, so a type-by-type scan of it is worth
  /// the reflection. The game and the runtime hold tens of thousands of types and no steps.</summary>
  /// <param name="assemblyName">The simple assembly name, as <c>GetName().Name</c> reports it.</param>
  /// <returns><c>true</c> when the assembly should be scanned.</returns>
  public static bool CouldHoldSteps(string? assemblyName) {
    string name = assemblyName?.Trim() ?? string.Empty;
    if (name.Length == 0) {
      return false;
    }

    foreach (string prefix in NotMods) {
      if (name.Equals(prefix, StringComparison.Ordinal)
          || name.StartsWith(prefix + ".", StringComparison.Ordinal)
          || name.StartsWith(prefix + "-", StringComparison.Ordinal)) {
        return false;
      }
    }

    return true;
  }
}
