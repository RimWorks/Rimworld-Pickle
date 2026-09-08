namespace RimWorks.Pickle.Vanilla;

/// <summary>Mod presence and load order. Presence shares <see cref="ModLookup"/> with
/// <c>@requires:</c>, so a tag and a step can never disagree about whether a mod is loaded.</summary>
[PickleSteps]
public class ModSteps {
  /// <summary>Asserts a mod is loaded.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="wanted">The mod name or packageId.</param>
  [Then("mod {string} is loaded")]
  public void AssertLoaded(PickleContext ctx, string wanted) {
    bool loaded = ModLookup.IsLoaded(wanted);
    ctx.Assert(loaded, loaded ? null : $"mod '{wanted}' should be loaded. loaded mods: {ModLookup.DescribeLoadOrder()}");
  }

  /// <summary>Asserts a mod is not loaded.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="wanted">The mod name or packageId.</param>
  [Then("mod {string} is not loaded")]
  public void AssertNotLoaded(PickleContext ctx, string wanted) {
    bool loaded = ModLookup.IsLoaded(wanted);
    ctx.Assert(!loaded, !loaded ? null : $"mod '{wanted}' should not be loaded. loaded mods: {ModLookup.DescribeLoadOrder()}");
  }

  /// <summary>Asserts one mod's load order comes before another's.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="first">The mod expected to load first.</param>
  /// <param name="second">The mod expected to load after <paramref name="first"/>.</param>
  [Then("mod {string} loads before {string}")]
  public void AssertLoadsBefore(PickleContext ctx, string first, string second) {
    int firstIndex = RequireIndex(ctx, first);
    int secondIndex = RequireIndex(ctx, second);
    bool ok = firstIndex < secondIndex;

    ctx.Assert(ok, ok ? null : $"'{first}' should load before '{second}'. load order: {ModLookup.DescribeLoadOrder()}");
  }

  /// <summary>Asserts one mod's load order comes after another's.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="first">The mod expected to load after <paramref name="second"/>.</param>
  /// <param name="second">The mod expected to load first.</param>
  [Then("mod {string} loads after {string}")]
  public void AssertLoadsAfter(PickleContext ctx, string first, string second) {
    int firstIndex = RequireIndex(ctx, first);
    int secondIndex = RequireIndex(ctx, second);
    bool ok = firstIndex > secondIndex;

    ctx.Assert(ok, ok ? null : $"'{first}' should load after '{second}'. load order: {ModLookup.DescribeLoadOrder()}");
  }

  // A missing mod cannot have a position to compare, so it is a broken precondition rather
  // than a failed order expectation - same split DlcSteps.RequireIdeo makes.
  private static int RequireIndex(PickleContext ctx, string wanted) {
    int? index = ModLookup.IndexOf(wanted);
    ctx.Require(
        index != null,
        index != null ? string.Empty : $"mod '{wanted}' is not loaded. loaded mods: {ModLookup.DescribeLoadOrder()}");
    return index!.Value;
  }
}
