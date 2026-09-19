using System.Collections.Generic;
using System.Runtime.CompilerServices;

namespace RimWorks.Pickle.Patching;

/// <summary>
/// A method each backend patches before Pickle trusts it. A library can accept a patch without
/// throwing and still never run it: Concord does when its own startup fails partway.
/// </summary>
public static class PatchProbe {
  // Per backend, because every backend hooks the same method and a working one would
  // otherwise make a broken one look fine.
  private static readonly Dictionary<string, int> Hits = new();

  /// <summary>Returns its argument. Backends attach an after-hook that calls <see cref="Record"/>.</summary>
  /// <param name="value">Any value, returned as is.</param>
  /// <returns><paramref name="value"/>.</returns>
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static int Target(int value) {
    return value;
  }

  /// <summary>Called from a backend's hook on <see cref="Target"/>.</summary>
  /// <param name="backend">The <see cref="IPatchBackend.Name"/> of the backend whose hook ran.</param>
  public static void Record(string backend) {
    Hits.TryGetValue(backend, out int count);
    Hits[backend] = count + 1;
  }

  /// <summary>Calls <see cref="Target"/> once and reports whether the backend's hook ran.</summary>
  /// <param name="backend">The <see cref="IPatchBackend.Name"/> of the backend to check.</param>
  /// <returns>True when that backend's hook ran during the call.</returns>
  public static bool Fired(string backend) {
    Hits.TryGetValue(backend, out int before);
    Target(before);
    Hits.TryGetValue(backend, out int after);
    return after != before;
  }
}
