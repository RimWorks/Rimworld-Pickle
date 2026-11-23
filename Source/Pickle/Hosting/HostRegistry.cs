using System;
using System.Collections.Concurrent;

namespace RimWorks.Pickle.Hosting;

/// <summary>
/// Holds the capabilities a host registers at boot. Each capability is one interface in this
/// namespace, and a host can leave any of them out.
/// </summary>
public static class HostRegistry {
  private static readonly ConcurrentDictionary<Type, object> Capabilities = new();

  /// <summary>Registers the host's implementation of a capability, replacing any earlier one.</summary>
  /// <typeparam name="T">The capability interface.</typeparam>
  /// <param name="capability">The host's implementation.</param>
  public static void Register<T>(T capability)
      where T : class {
    Capabilities[typeof(T)] = capability ?? throw new ArgumentNullException(nameof(capability));
  }

  /// <summary>Looks up the host's implementation of a capability without throwing.</summary>
  /// <typeparam name="T">The capability interface.</typeparam>
  /// <param name="capability">The registered implementation, or null when the host did not register one.</param>
  /// <returns>True when the host registered an implementation of <typeparamref name="T"/>.</returns>
  public static bool TryGet<T>(out T? capability)
      where T : class {
    if (Capabilities.TryGetValue(typeof(T), out object? registered)) {
      capability = (T)registered;
      return true;
    }

    capability = null;
    return false;
  }

  /// <summary>Returns the host's implementation of a capability.</summary>
  /// <typeparam name="T">The capability interface.</typeparam>
  /// <returns>The registered implementation.</returns>
  /// <exception cref="InvalidOperationException">The host registered no implementation of <typeparamref name="T"/>.</exception>
  public static T Get<T>()
      where T : class {
    if (Capabilities.TryGetValue(typeof(T), out object? capability)) {
      return (T)capability;
    }

    throw new InvalidOperationException(
        $"The host does not provide the capability {typeof(T).Name}.");
  }
}
