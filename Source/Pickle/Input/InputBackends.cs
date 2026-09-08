using System;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Input;

/// <summary>
/// Picks the one input backend that can work on this platform and holds it for the session.
/// </summary>
public static class InputBackends {
  /// <summary>The backend selected for this platform, chosen once at startup.</summary>
  public static IInputBackend Current { get; } = Select();

  /// <summary>Why <see cref="Current"/> cannot drive input, or <c>null</c> when it can.</summary>
  public static string? UnavailableReason => Current.UnavailableReason;

  /// <summary>Whether <see cref="Current"/> can drive input on this machine.</summary>
  public static bool Available => Current.UnavailableReason == null;

  /// <summary>Converts a GUI-space point to the screen space a backend expects.</summary>
  /// <param name="guiPoint">The point in GUI space.</param>
  /// <returns>The equivalent point in screen space.</returns>
  // The one place GUI space becomes screen space. Both are top-left, so this only applies
  // UIScale; add an offset here if input lands wrong.
  public static Vector2 ToScreen(Vector2 guiPoint) {
    float scale = Prefs.UIScale;
    return new Vector2(guiPoint.x * scale, guiPoint.y * scale);
  }

  /// <summary>Throws when <see cref="Current"/> cannot drive input, naming why.</summary>
  public static void EnsureAvailable() {
    if (Current.UnavailableReason != null) {
      throw new InvalidOperationException(Current.UnavailableReason);
    }
  }

  // Anything not Windows falls to xdotool, which then reports whether this really is an X11
  // session. That keeps the "wrong platform" message in one place.
  private static IInputBackend Select() {
    return Environment.OSVersion.Platform == PlatformID.Win32NT
        ? new SendInputBackend()
        : new XdoInput();
  }
}
