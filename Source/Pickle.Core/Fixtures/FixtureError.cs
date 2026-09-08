namespace RimWorks.Pickle.Core.Fixtures;

/// <summary>Why <see cref="FixtureResolver"/> could not resolve a fixture name to a file.</summary>
public enum FixtureErrorKind {
  /// <summary>No suite has a fixture by this name.</summary>
  NotFound,

  /// <summary>More than one other suite has a fixture by this name, so none of them wins.</summary>
  Duplicate,
}

/// <summary>Why resolving a fixture name failed, with a message ready to show the user.</summary>
public class FixtureError {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="fixtureName">The name that failed to resolve.</param>
  /// <param name="kind">Why the lookup failed.</param>
  /// <param name="message">The message to show, already naming the known or clashing fixtures.</param>
  public FixtureError(string fixtureName, FixtureErrorKind kind, string message) {
    FixtureName = fixtureName;
    Kind = kind;
    Message = message;
  }

  /// <summary>The name that failed to resolve.</summary>
  public string FixtureName { get; }

  /// <summary>Why the lookup failed.</summary>
  public FixtureErrorKind Kind { get; }

  /// <summary>The message to show the user.</summary>
  public string Message { get; }
}
