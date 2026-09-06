using System;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Input;

/// <summary>
/// Picks the one input backend that can work on this platform and holds it for the session.
/// </summary>
public static class InputBackends {
  public static IInputBackend Current { get; } = Select();

  public static string? UnavailableReason => Current.UnavailableReason;

  public static bool Available => Current.UnavailableReason == null;

  // The one place GUI space becomes screen space. Both are top-left, so this only applies
  // UIScale; add an offset here if input lands wrong.
  public static Vector2 ToScreen(Vector2 guiPoint) {
    float scale = Prefs.UIScale;
    return new Vector2(guiPoint.x * scale, guiPoint.y * scale);
  }

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
