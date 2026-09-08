namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>The outcome of <see cref="FixtureResolver.Resolve"/>, either a fixture or why one was not found.</summary>
public class FixtureResolution {
  /// <summary>Initializes a new instance for a successful resolution.</summary>
  /// <param name="fixture">The fixture that was found.</param>
  public FixtureResolution(ResolvedFixture fixture) {
    Fixture = fixture;
    Error = null;
  }

  /// <summary>Initializes a new instance for a failed resolution.</summary>
  /// <param name="error">Why the fixture could not be resolved.</param>
  public FixtureResolution(FixtureError error) {
    Fixture = null;
    Error = error;
  }

  /// <summary>The fixture that was found, or <c>null</c> when resolution failed.</summary>
  public ResolvedFixture? Fixture { get; }

  /// <summary>Why resolution failed, or <c>null</c> when it succeeded.</summary>
  public FixtureError? Error { get; }
}
