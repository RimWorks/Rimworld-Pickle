using System;

namespace RimWorks.Pickle;

public class PickleRequireException : Exception {
  public PickleRequireException(string message) : base(message) {
  }

  public PickleRequireException(string message, Exception innerException) : base(message, innerException) {
  }
}
