using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace Pickle.Vanilla;

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

  private static string DescribeMissing(string defTypeName, string requested, IEnumerable<string> candidateNames) {
    List<string> closest = [.. candidateNames
        .OrderBy(name => LevenshteinDistance(name, requested))
        .Take(3)];

    return $"no {defTypeName} named '{requested}'. closest matches: {string.Join(", ", closest)}";
  }

  private static int LevenshteinDistance(string a, string b) {
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
}
