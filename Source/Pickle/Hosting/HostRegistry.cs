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
        $"The host does not provide the capability {typeof(T).Name}, so this step cannot run here.");
  }
}
