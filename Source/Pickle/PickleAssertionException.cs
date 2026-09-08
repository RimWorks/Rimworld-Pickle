using System;

namespace RimWorks.Pickle;

/// <summary>Thrown by <see cref="PickleContext.Assert"/> when a condition does not hold.</summary>
public class PickleAssertionException : Exception {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="message">The label passed to the failed assert.</param>
  public PickleAssertionException(string message) : base(message) {
  }

  /// <summary>Initializes a new instance.</summary>
  /// <param name="message">The label passed to the failed assert.</param>
  /// <param name="innerException">The exception that caused the assertion to fail.</param>
  public PickleAssertionException(string message, Exception innerException) : base(message, innerException) {
  }
}
