namespace RimWorks.Pickle.Core.Fixtures;

public class FixtureResolution {
  public FixtureResolution(ResolvedFixture fixture) {
    Fixture = fixture;
    Error = null;
  }

  public FixtureResolution(FixtureError error) {
    Fixture = null;
    Error = error;
  }

  public ResolvedFixture? Fixture { get; }

  public FixtureError? Error { get; }
}
