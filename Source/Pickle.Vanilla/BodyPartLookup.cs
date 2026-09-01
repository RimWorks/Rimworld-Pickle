using System;
using System.Collections.Generic;
using System.Linq;
using Verse;

namespace RimWorks.Pickle.Vanilla;

internal static class BodyPartLookup {
  public static BodyPartRecord Require(Pawn pawn, string label) {
    List<BodyPartRecord> matches = [.. AllParts(pawn).Where(part => Matches(part, label))];

    if (matches.Count == 1) {
      return matches[0];
    }

    // A human has two shoulders and ten fingers, so the bare def label matches more than one.
    if (matches.Count > 1) {
      string found = string.Join(", ", matches.Select(p => p.Label).Distinct());
      throw new InvalidOperationException(
          $"'{label}' names {matches.Count} parts on '{pawn.LabelShort}': {found}. say which one");
    }

    throw new InvalidOperationException(DescribeMissing(pawn, label));
  }

  public static string Describe(Pawn pawn) {
    List<string> missing = [.. pawn.health.hediffSet.GetMissingPartsCommonAncestors().Select(h => h.Part.Label)];
    string gone = missing.Count == 0 ? "(none)" : string.Join(", ", missing);
    return $"missing parts: {gone}";
  }

  private static IEnumerable<BodyPartRecord> AllParts(Pawn pawn) {
    return pawn.RaceProps?.body?.AllParts ?? Enumerable.Empty<BodyPartRecord>();
  }

  // untranslatedCustomLabel comes first so a scenario written in English still matches on a
  // game running in another language.
  private static bool Matches(BodyPartRecord part, string label) {
    return Same(part.untranslatedCustomLabel, label)
        || Same(part.Label, label)
        || Same(part.def?.label, label)
        || Same(part.def?.defName, label);
  }

  private static bool Same(string? a, string b) {
    return a != null && string.Equals(a, b, StringComparison.OrdinalIgnoreCase);
  }

  private static string DescribeMissing(Pawn pawn, string requested) {
    List<string> closest = [.. AllParts(pawn)
        .Select(p => p.Label)
        .Distinct()
        .OrderBy(name => DefLookup.LevenshteinDistance(name, requested))
        .Take(3)];

    string suggestions = closest.Count == 0 ? "(the pawn has no body)" : string.Join(", ", closest);
    return $"'{pawn.LabelShort}' has no body part named '{requested}'. closest matches: {suggestions}";
  }
}
