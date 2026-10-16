using System;
using System.IO;
using System.Net;

namespace RimWorks.Pickle.Core.Web;

/// <summary>
/// Tells a browser hanging up mid-response apart from a request that really failed.
/// </summary>
public static class ClientHangup {
  /// <summary>
  /// True when this exception, or anything it wraps, is a dropped connection.
  /// </summary>
  /// <param name="ex">The exception a response threw, or null.</param>
  /// <returns>True when the client hung up rather than the request failing.</returns>
  public static bool Caused(Exception? ex) {
    for (Exception? e = ex; e != null; e = e.InnerException) {
      if (e is HttpListenerException || e is IOException || e is ObjectDisposedException) {
        return true;
      }
    }

    return false;
  }
}
