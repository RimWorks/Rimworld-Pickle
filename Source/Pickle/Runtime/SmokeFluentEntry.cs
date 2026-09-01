namespace RimWorks.Pickle.Runtime;

[PickleEntry]
public static class SmokeFluentEntry {
  public static void Init() {
    Pickle.Given("fluent smoke step passes", ctx => ctx.Assert(true, "fluent smoke pass"));
  }
}
