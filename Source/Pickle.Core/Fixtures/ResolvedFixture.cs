namespace Pickle.Core.Fixtures;

public class ResolvedFixture {
  public ResolvedFixture(string fullPath) {
    FullPath = fullPath;
  }

  public string FullPath { get; }
}
