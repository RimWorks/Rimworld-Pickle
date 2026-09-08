namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>A fixture name resolved to the one file that should be loaded.</summary>
public class ResolvedFixture {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="fullPath">The path to the fixture file.</param>
  public ResolvedFixture(string fullPath) {
    FullPath = fullPath;
  }

  /// <summary>The path to the fixture file.</summary>
  public string FullPath { get; }
}
