namespace RimWorks.Pickle.Runtime;

/// <summary>Registers a fixed passing step through the fluent <see cref="Pickle"/> API, to smoke test it.</summary>
[PickleEntry]
public static class SmokeFluentEntry {
  /// <summary>Registers the smoke step. Called once by whatever discovers <see cref="PickleEntryAttribute"/>.</summary>
  public static void Init() {
    Pickle.Given("fluent smoke step passes", ctx => ctx.Assert(true, "fluent smoke pass"));
  }
}
