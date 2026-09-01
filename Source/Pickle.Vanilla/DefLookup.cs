using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorks.Pickle.Vanilla;

internal static class DefLookup {
  public static T Require<T>(string defName)
    where T : Def {
    T? def = DefDatabase<T>.GetNamedSilentFail(defName);
    if (def != null) {
      return def;
    }

    throw new InvalidOperationException(DescribeMissing(
        typeof(T).Name, defName, DefDatabase<T>.AllDefsListForReading.Select(d => d.defName)));
  }

  /// <summary>
  /// Finds a def by name across every database, because a feature file names a def
  /// without knowing which database holds it.
  /// </summary>
  public static List<Def> FindAll(string defName) {
    List<Def> found = [];

    foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases()) {
      Def? def = GenDefDatabase.GetDefSilentFail(defType, defName, false);
      if (def != null) {
        found.Add(def);
      }
    }

    return found;
  }

  public static Def RequireAny(string defName) {
    List<Def> found = FindAll(defName);

    if (found.Count == 1) {
      return found[0];
    }

    if (found.Count > 1) {
      string types = string.Join(", ", found.Select(d => d.GetType().Name));
      throw new InvalidOperationException(
          $"'{defName}' names more than one def ({types}); " +
          $"say which with 'def \"{defName}\" of type \"...\"'");
    }

    throw new InvalidOperationException(DescribeMissingAnywhere(defName));
  }

  public static Def? FindOfType(string defName, string defTypeName) {
    return GenDefDatabase.GetDefSilentFail(RequireDefType(defTypeName), defName, false);
  }

  public static string DescribeMissingAnywhere(string requested) {
    List<Def> all = [];
    foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases()) {
      foreach (Def def in GenDefDatabase.GetAllDefsInDatabaseForDef(defType)) {
        all.Add(def);
      }
    }

    List<string> closest = [.. all
        .OrderBy(d => LevenshteinDistance(d.defName, requested))
        .Take(3)
        .Select(d => $"{d.defName} ({d.GetType().Name})")];

    string suggestions = closest.Count == 0 ? "(no defs are loaded)" : string.Join(", ", closest);
    return $"no def named '{requested}' in any database. closest matches: {suggestions}";
  }

  public static string DescribeMissingInType(string defTypeName, string requested) {
    Type defType = RequireDefType(defTypeName);
    IEnumerable<string> names = GenDefDatabase.GetAllDefsInDatabaseForDef(defType)
        .Cast<Def>()
        .Select(d => d.defName);

    return DescribeMissing(defTypeName, requested, names);
  }

  internal static int LevenshteinDistance(string a, string b) {
    int[,] distances = new int[a.Length + 1, b.Length + 1];

    for (int i = 0; i <= a.Length; i++) {
      distances[i, 0] = i;
    }

    for (int j = 0; j <= b.Length; j++) {
      distances[0, j] = j;
    }

    for (int i = 1; i <= a.Length; i++) {
      for (int j = 1; j <= b.Length; j++) {
        int cost = char.ToLowerInvariant(a[i - 1]) == char.ToLowerInvariant(b[j - 1]) ? 0 : 1;
        distances[i, j] = Math.Min(
            Math.Min(distances[i - 1, j] + 1, distances[i, j - 1] + 1),
            distances[i - 1, j - 1] + cost);
      }
    }

    return distances[a.Length, b.Length];
  }

  private static Type RequireDefType(string defTypeName) {
    foreach (Type defType in GenDefDatabase.AllDefTypesWithDatabases()) {
      if (string.Equals(defType.Name, defTypeName, StringComparison.OrdinalIgnoreCase)) {
        return defType;
      }
    }

    List<string> closest = [.. GenDefDatabase.AllDefTypesWithDatabases()
        .Cast<Type>()
        .OrderBy(t => LevenshteinDistance(t.Name, defTypeName))
        .Take(3)
        .Select(t => t.Name)];

    throw new InvalidOperationException(
        $"no def database for type '{defTypeName}'. closest matches: {string.Join(", ", closest)}");
  }

  private static string DescribeMissing(string defTypeName, string requested, IEnumerable<string> candidateNames) {
    List<string> closest = [.. candidateNames
        .OrderBy(name => LevenshteinDistance(name, requested))
        .Take(3)];

    return $"no {defTypeName} named '{requested}'. closest matches: {string.Join(", ", closest)}";
  }
}
