using System;

namespace RimWorks.Pickle;

/// <summary>Thrown by <see cref="PickleContext.Require"/> when a step's setup precondition is not met,
/// as opposed to <see cref="PickleAssertionException"/> for a failed expectation.</summary>
public class PickleRequireException : Exception {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="message">What was required and was not there.</param>
  public PickleRequireException(string message) : base(message) {
  }

  /// <summary>Initializes a new instance.</summary>
  /// <param name="message">What was required and was not there.</param>
  /// <param name="innerException">The exception that caused the precondition to fail.</param>
  public PickleRequireException(string message, Exception innerException) : base(message, innerException) {
  }
}
