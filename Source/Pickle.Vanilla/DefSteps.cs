using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorks.Pickle.Patching;
using RimWorld;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// Reads defs straight out of the database. None of these need a save, so an XML-only
/// mod gets a suite that runs in under a second instead of waiting out a fixture load.
/// </summary>
[PickleSteps]
public class DefSteps {
  [Then("def {string} exists")]
  public void AssertDefExists(PickleContext ctx, string defName) {
    bool found = DefLookup.FindAll(defName).Count > 0;
    ctx.Assert(found, found ? null : DefLookup.DescribeMissingAnywhere(defName));
  }

  [Then("no def {string} exists")]
  public void AssertDefAbsent(PickleContext ctx, string defName) {
    List<Def> found = DefLookup.FindAll(defName);
    ctx.Assert(
        found.Count == 0,
        $"expected no def named '{defName}'; found {string.Join(", ", found.Select(d => d.GetType().Name))}");
  }

  [Then("def {string} of type {string} exists")]
  public void AssertDefOfTypeExists(PickleContext ctx, string defName, string defTypeName) {
    Def? def = DefLookup.FindOfType(defName, defTypeName);
    ctx.Assert(def != null, def != null ? null : DefLookup.DescribeMissingInType(defTypeName, defName));
  }

  [Then("def {string} is defined by mod {string}")]
  public void AssertDefOwner(PickleContext ctx, string defName, string modName) {
    Def def = DefLookup.RequireAny(defName);
    ModContentPack? owner = def.modContentPack;

    bool matches = owner != null
        && (string.Equals(owner.Name, modName, StringComparison.OrdinalIgnoreCase)
            || string.Equals(owner.PackageId, modName, StringComparison.OrdinalIgnoreCase));

    ctx.Assert(
        matches,
        $"def '{defName}' should be defined by '{modName}'; " +
        $"actual {owner?.Name ?? "(no mod)"} ({owner?.PackageId ?? "no packageId"})");
  }

  [Then("def {string} field {string} is {string}")]
  public void AssertDefField(PickleContext ctx, string defName, string fieldPath, string expected) {
    Def def = DefLookup.RequireAny(defName);
    object? value = ResolvePath(ctx, def, fieldPath);
    string actual = value?.ToString() ?? "(null)";

    ctx.Assert(
        string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        $"def '{defName}' field '{fieldPath}' should be '{expected}'; actual '{actual}'");
  }

  [Then("def {string} costs {int} {string}")]
  public void AssertDefCost(PickleContext ctx, string defName, int expected, string costDefName) {
    BuildableDef def = RequireBuildable(ctx, defName, "a costList");
    ThingDef cost = DefLookup.Require<ThingDef>(costDefName);
    int actual = def.costList?.Where(c => c.thingDef == cost).Sum(c => c.count) ?? 0;

    ctx.Assert(
        actual == expected,
        $"def '{defName}' should cost {expected} {costDefName}; actual {actual}. " +
        $"costList: {DescribeCostList(def)}");
  }

  // The computed value, not the statBases entry, because a stat the def never lists
  // still has a defaultBaseValue that the game happily uses.
  [Then("def {string} stat {string} is {float}")]
  public void AssertDefStat(PickleContext ctx, string defName, string statDefName, float expected) {
    BuildableDef def = RequireBuildable(ctx, defName, "stats");
    StatDef stat = DefLookup.Require<StatDef>(statDefName);
    float actual = def.GetStatValueAbstract(stat);

    ctx.Assert(
        StatTolerance.IsNear(actual, expected),
        $"def '{defName}' stat '{statDefName}' should be {expected} within " +
        $"{StatTolerance.For(expected):G3}; actual {actual}. this is the computed value, " +
        $"made without stuff. statBases: {DescribeStatBases(def)}");
  }

  [Then("def {string} raw stat {string} is {float}")]
  public void AssertDefRawStat(PickleContext ctx, string defName, string statDefName, float expected) {
    BuildableDef def = RequireBuildable(ctx, defName, "statBases");
    StatDef stat = DefLookup.Require<StatDef>(statDefName);
    StatModifier? entry = def.statBases?.FirstOrDefault(m => m.stat == stat);

    ctx.Require(
        entry != null,
        $"def '{defName}' has no statBases entry for '{statDefName}'; it falls back to the " +
        $"stat default of {stat.defaultBaseValue}. statBases: {DescribeStatBases(def)}");

    ctx.Assert(
        StatTolerance.IsNear(entry!.value, expected),
        $"def '{defName}' statBases '{statDefName}' should be {expected} within " +
        $"{StatTolerance.For(expected):G3}; actual {entry.value}");
  }

  [Then("def {string} was patched by mod {string}")]
  public void AssertDefPatchedBy(PickleContext ctx, string defName, string modName) {
    RequirePatchable(ctx, defName);

    IReadOnlyCollection<string> patchers = PatchAttribution.PatchersOf(defName);
    ctx.Assert(
        patchers.Any(m => string.Equals(m, modName, StringComparison.OrdinalIgnoreCase)),
        $"def '{defName}' should have been patched by '{modName}'; patched by {Describe(patchers)}");
  }

  [Then("def {string} was patched")]
  public void AssertDefPatched(PickleContext ctx, string defName) {
    RequirePatchable(ctx, defName);

    IReadOnlyCollection<string> patchers = PatchAttribution.PatchersOf(defName);
    ctx.Assert(patchers.Count > 0, $"def '{defName}' was not patched by any mod");
  }

  [Then("no def {string} was patched")]
  public void AssertDefNotPatched(PickleContext ctx, string defName) {
    RequireAttribution(ctx);

    IReadOnlyCollection<string> patchers = PatchAttribution.PatchersOf(defName);
    ctx.Assert(patchers.Count == 0, $"def '{defName}' was patched by {Describe(patchers)}");
  }

  // A patch targets a defName, not a database, so a name held by two def types is fine
  // here even though the other def steps reject it.
  private static void RequirePatchable(PickleContext ctx, string defName) {
    RequireAttribution(ctx);
    ctx.Require(DefLookup.FindAll(defName).Count > 0, DefLookup.DescribeMissingAnywhere(defName));
  }

  // Reporting "not patched" when the hook never installed would be a wrong answer rather
  // than a failure, so say so instead.
  private static void RequireAttribution(PickleContext ctx) {
    ctx.Require(
        PatchAttribution.Armed,
        "patch attribution never installed, so no def can be reported as patched. " +
        "Pickle needs Harmony or Concord loaded before the game applies XML patches");
  }

  private static string Describe(IReadOnlyCollection<string> patchers) {
    return patchers.Count == 0 ? "(no mod)" : string.Join(", ", patchers);
  }

  private static BuildableDef RequireBuildable(PickleContext ctx, string defName, string what) {
    Def def = DefLookup.RequireAny(defName);
    ctx.Require(
        def is BuildableDef,
        $"def '{defName}' is a {def.GetType().Name}, which has no {what}");

    return (BuildableDef)def;
  }

  // Walks public fields then properties, one path segment at a time. A miss names what
  // was available where the walk stopped, so a typo does not read as a wrong value.
  private static object? ResolvePath(PickleContext ctx, Def def, string fieldPath) {
    object? current = def;

    foreach (string segment in fieldPath.Split('.')) {
      ctx.Require(current != null, $"'{fieldPath}' walks through a null at '{segment}'");

      Type type = current!.GetType();
      FieldInfo? field = type.GetField(segment, BindingFlags.Public | BindingFlags.Instance);
      if (field != null) {
        current = field.GetValue(current);
        continue;
      }

      PropertyInfo? property = type.GetProperty(segment, BindingFlags.Public | BindingFlags.Instance);
      ctx.Require(
          property != null,
          $"{type.Name} has no field or property '{segment}'. available: {DescribeMembers(type)}");

      current = property!.GetValue(current);
    }

    return current;
  }

  private static string DescribeMembers(Type type) {
    List<string> names = [.. type
        .GetFields(BindingFlags.Public | BindingFlags.Instance)
        .Select(f => f.Name)
        .OrderBy(n => n, StringComparer.OrdinalIgnoreCase)
        .Take(12)];

    return names.Count == 0 ? "(none)" : string.Join(", ", names);
  }

  private static string DescribeCostList(BuildableDef def) {
    List<string> costs = [.. def.costList?.Select(c => $"{c.count}x {c.thingDef?.defName}") ?? []];
    return costs.Count == 0 ? "(empty)" : string.Join(", ", costs);
  }

  private static string DescribeStatBases(BuildableDef def) {
    List<string> stats = [.. def.statBases?.Select(m => $"{m.stat?.defName}={m.value}") ?? []];
    return stats.Count == 0 ? "(empty)" : string.Join(", ", stats);
  }
}
