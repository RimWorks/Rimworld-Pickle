namespace Pickle.Core.Fixtures;

public enum FixtureErrorKind {
  NotFound,
  Duplicate,
}

public class FixtureError {
  public FixtureError(string fixtureName, FixtureErrorKind kind, string message) {
    FixtureName = fixtureName;
    Kind = kind;
    Message = message;
  }

  public string FixtureName { get; }

  public FixtureErrorKind Kind { get; }

  public string Message { get; }
}
