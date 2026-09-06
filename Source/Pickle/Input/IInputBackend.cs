using UnityEngine;

namespace RimWorks.Pickle.Input;

/// <summary>
/// One OS-level input path. Exactly one is viable per platform, so implementations are
/// selected by a platform check rather than ranked against each other.
/// </summary>
public interface IInputBackend {
  /// <summary>Null when this backend can drive input, otherwise why it cannot.</summary>
  string? UnavailableReason { get; }

  /// <summary>Moves the pointer to a GUI-space point.</summary>
  /// <param name="guiPoint">The point in GUI space.</param>
  void MoveTo(Vector2 guiPoint);

  /// <summary>Clicks at a GUI-space point.</summary>
  /// <param name="guiPoint">The point in GUI space.</param>
  /// <param name="button">The mouse button, 1 for left.</param>
  void Click(Vector2 guiPoint, int button = 1);

  /// <summary>Presses and releases a key by its Pickle name, such as Escape or a.</summary>
  /// <param name="keyName">The key name a step wrote.</param>
  void Key(string keyName);

  /// <summary>Where the pointer is now, for a failure message.</summary>
  /// <returns>A human-readable pointer location.</returns>
  string GetMouseLocation();
}
