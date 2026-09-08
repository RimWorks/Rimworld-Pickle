using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Input;

/// <summary>
/// SendInput is the Windows analogue of XTEST: it enters the system input stream below the
/// app, so Unity's polled Input sees it the same way it sees a real device.
/// </summary>
public sealed class SendInputBackend : IInputBackend {
  private const uint InputMouse = 0;
  private const uint InputKeyboard = 1;

  private const uint KeyEventExtended = 0x0001;
  private const uint KeyEventKeyUp = 0x0002;

  private const uint MouseEventMove = 0x0001;
  private const uint MouseEventAbsolute = 0x8000;

  private const uint MapVkToVsc = 0;

  private const int SmCxScreen = 0;
  private const int SmCyScreen = 1;

  /// <summary>Initializes a new instance, probing for an interactive desktop right away.</summary>
  public SendInputBackend() {
    UnavailableReason = Probe();
  }

  /// <inheritdoc/>
  public string? UnavailableReason { get; }

  /// <inheritdoc/>
  public void MoveTo(Vector2 guiPoint) {
    EnsureUsable();
    SendMouseMove(InputBackends.ToScreen(guiPoint));
  }

  /// <inheritdoc/>
  public void Click(Vector2 guiPoint, int button = 1) {
    EnsureUsable();
    (uint down, uint up) = ButtonFlags(button);

    Vector2 screen = InputBackends.ToScreen(guiPoint);
    SendMouseMove(screen);

    // A click that resolves its tag and then activates nothing is the failure this path
    // has, so every send records what it aimed at and where the pointer ended up.
    Log.InfoTo("Pickle",
        "sendinput click gui={Gui} client={Client} desktop={Desktop} metrics={Width}x{Height} cursor={Cursor}",
        [guiPoint, screen, ToDesktop(screen), GetSystemMetrics(SmCxScreen), GetSystemMetrics(SmCyScreen), GetMouseLocation()]);

    Send(MouseEvent(down));
    Send(MouseEvent(up));
  }

  /// <inheritdoc/>
  // Raw input readers key off the scan code, so wScan is filled even though wVk is set.
  // A VK-only event is the classic "the game ignored my key" bug.
  public void Key(string keyName) {
    EnsureUsable();
    (ushort virtualKey, bool extended) = MapVirtualKeyCode(keyName);
    uint flags = extended ? KeyEventExtended : 0;

    Send(KeyEvent(virtualKey, flags));
    Send(KeyEvent(virtualKey, flags | KeyEventKeyUp));
  }

  /// <inheritdoc/>
  public string GetMouseLocation() {
    return GetCursorPos(out Point point) ? $"x:{point.X} y:{point.Y}" : "(GetCursorPos failed)";
  }

  private static (uint Down, uint Up) ButtonFlags(int button) {
    return button switch {
      1 => (0x0002u, 0x0004u),
      2 => (0x0020u, 0x0040u),
      3 => (0x0008u, 0x0010u),
      _ => throw new ArgumentOutOfRangeException(nameof(button), button, "Mouse button must be 1, 2 or 3."),
    };
  }

  // Delete is the only extended key in the supported set; arrows and the numpad would need
  // the same flag if they are ever added.
  private static (ushort VirtualKey, bool Extended) MapVirtualKeyCode(string keyName) {
    return keyName.ToLowerInvariant() switch {
      "escape" => ((ushort)0x1B, false),
      "return" or "enter" => ((ushort)0x0D, false),
      "space" => ((ushort)0x20, false),
      "tab" => ((ushort)0x09, false),
      "delete" => ((ushort)0x2E, true),
      "backspace" => ((ushort)0x08, false),
      _ when keyName.Length == 1 && char.IsLetter(keyName[0]) => ((ushort)char.ToUpperInvariant(keyName[0]), false),
      _ when keyName.Length == 1 && char.IsDigit(keyName[0]) => ((ushort)keyName[0], false),
      _ => throw KeyNames.Unknown(keyName),
    };
  }

  private static Input1 KeyEvent(ushort virtualKey, uint flags) {
    Input1 input = default;
    input.Type = InputKeyboard;
    input.Union.Keyboard.VirtualKey = virtualKey;
    input.Union.Keyboard.ScanCode = (ushort)MapVirtualKey(virtualKey, MapVkToVsc);
    input.Union.Keyboard.Flags = flags;
    return input;
  }

  private static Input1 MouseEvent(uint flags) {
    Input1 input = default;
    input.Type = InputMouse;
    input.Union.Mouse.Flags = flags;
    return input;
  }

  private static void Send(Input1 input) {
    uint accepted = SendInput(1, [input], Marshal.SizeOf(typeof(Input1)));
    if (accepted == 0) {
      throw new InvalidOperationException(
          $"SendInput was refused (error {Marshal.GetLastWin32Error()}); the desktop may be locked or on another session.");
    }
  }

  // Absolute coordinates are 0-65535 over the primary monitor, not pixels. The full
  // desktop would need MOUSEEVENTF_VIRTUALDESK on top.
  private static void SendMouseMove(Vector2 clientPoint) {
    Vector2 desktop = ToDesktop(clientPoint);
    int width = GetSystemMetrics(SmCxScreen);
    int height = GetSystemMetrics(SmCyScreen);

    Input1 input = default;
    input.Type = InputMouse;
    input.Union.Mouse.X = (int)(desktop.x * 65535 / width);
    input.Union.Mouse.Y = (int)(desktop.y * 65535 / height);
    input.Union.Mouse.Flags = MouseEventMove | MouseEventAbsolute;
    Send(input);
  }

  // A GUI point is relative to the client area, which starts below the title bar whenever
  // the window is decorated, but SendInput takes desktop coordinates. xdotool never needed
  // this because --window makes it window-relative already.
  private static Vector2 ToDesktop(Vector2 clientPoint) {
    Point point = new() { X = (int)clientPoint.x, Y = (int)clientPoint.y };
    return ClientToScreen(GetActiveWindow(), ref point)
        ? new Vector2(point.X, point.Y)
        : clientPoint;
  }

  // A zero screen means no interactive desktop, which is the one case SendInput cannot
  // reach: a service, session 0, or a locked machine.
  private static string? Probe() {
    return GetSystemMetrics(SmCxScreen) > 0 && GetSystemMetrics(SmCyScreen) > 0
        ? null
        : "there is no interactive desktop to inject into; SendInput cannot reach a locked or session 0 desktop";
  }

  [DllImport("user32.dll", SetLastError = true)]
  private static extern uint SendInput(uint count, Input1[] inputs, int size);

  [DllImport("user32.dll")]
  private static extern uint MapVirtualKey(uint code, uint mapType);

  [DllImport("user32.dll")]
  private static extern int GetSystemMetrics(int index);

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool GetCursorPos(out Point point);

  [DllImport("user32.dll")]
  private static extern IntPtr GetActiveWindow();

  [DllImport("user32.dll")]
  [return: MarshalAs(UnmanagedType.Bool)]
  private static extern bool ClientToScreen(IntPtr window, ref Point point);

  private void EnsureUsable() {
    if (UnavailableReason != null) {
      throw new InvalidOperationException(UnavailableReason);
    }
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct Point {
    public int X;
    public int Y;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct Input1 {
    public uint Type;
    public InputUnion Union;
  }

  // MOUSEINPUT and KEYBDINPUT overlap in the real INPUT struct, so the layout is explicit.
  [StructLayout(LayoutKind.Explicit)]
  private struct InputUnion {
    [FieldOffset(0)]
    public MouseInput Mouse;

    [FieldOffset(0)]
    public KeyboardInput Keyboard;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct MouseInput {
    public int X;
    public int Y;
    public uint Data;
    public uint Flags;
    public uint Time;
    public IntPtr ExtraInfo;
  }

  [StructLayout(LayoutKind.Sequential)]
  private struct KeyboardInput {
    public ushort VirtualKey;
    public ushort ScanCode;
    public uint Flags;
    public uint Time;
    public IntPtr ExtraInfo;
  }
}
