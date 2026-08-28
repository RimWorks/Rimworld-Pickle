using System;

namespace Pickle;

public class PickleAssertionException : Exception {
  public PickleAssertionException(string message) : base(message) {
  }

  public PickleAssertionException(string message, Exception innerException) : base(message, innerException) {
  }
}
