using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Input;

/// <summary>
/// XTEST input via xdotool is indistinguishable from a human mouse at the X11 level, so
/// it reaches native dispatch. The pointer really moves; windowed mode isolates it.
/// </summary>
public sealed class XdoInput : IInputBackend {
  private const int TimeoutMs = 2000;

  private readonly string xdotoolPath = ResolveXdotool();

  private string? gameWindowId;

  public XdoInput() {
    UnavailableReason = Probe();
  }

  public string? UnavailableReason { get; }

  // no --sync: it waits for a motion event, so a repeat click at the same spot hangs.
  public void MoveTo(Vector2 guiPoint) {
    Vector2 target = InputBackends.ToScreen(guiPoint);
    Run($"mousemove {WindowArg()} {(int)target.x} {(int)target.y}");
  }

  public void Click(Vector2 guiPoint, int button = 1) {
    Vector2 target = InputBackends.ToScreen(guiPoint);
    Run($"mousemove {WindowArg()} {(int)target.x} {(int)target.y} click {button}");
  }

  // Keys go to the focus window, not the pointer, and Xvfb has no WM to set it. Never pass
  // --window here: that switches xdotool to XSendEvent, which Unity ignores.
  public void Key(string keyName) {
    gameWindowId ??= FindGameWindow();
    string focus = gameWindowId == null ? string.Empty : $"windowfocus {gameWindowId} ";
    Run($"{focus}key {MapKeysym(keyName)}");
  }

  // Callers report this alongside the intended target when a click fails to land: it
  // separates a coordinate mapping bug from a click that went to the right place.
  public string GetMouseLocation() {
    return Capture("getmouselocation");
  }

  // X11 keysyms, not KeyCode names. Only space and BackSpace differ from the plain name.
  private static string MapKeysym(string keyName) {
    return keyName.ToLowerInvariant() switch {
      "escape" => "Escape",
      "return" or "enter" => "Return",
      "space" => "space",
      "tab" => "Tab",
      "delete" => "Delete",
      "backspace" => "BackSpace",
      _ when keyName.Length == 1 && char.IsLetter(keyName[0]) => keyName.ToLowerInvariant(),
      _ when keyName.Length == 1 && char.IsDigit(keyName[0]) => keyName,
      _ => throw KeyNames.Unknown(keyName),
    };
  }

  // Walks PATH itself rather than passing a bare name to the process launcher, so the
  // binary Pickle drives input with is fixed at startup and cannot be shadowed later.
  private static string ResolveXdotool() {
    string search = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    foreach (string dir in search.Split(System.IO.Path.PathSeparator)) {
      if (dir.Length == 0) {
        continue;
      }

      string candidate = System.IO.Path.Combine(dir, "xdotool");
      if (System.IO.File.Exists(candidate)) {
        return candidate;
      }
    }

    return string.Empty;
  }

  // Coordinates are relative to the game window, not the X screen. They match under
  // Xvfb only because the window sits at the origin.
  private string WindowArg() {
    gameWindowId ??= FindGameWindow();
    return gameWindowId == null ? string.Empty : $"--window {gameWindowId}";
  }

  private string? FindGameWindow() {
    string output = Capture("search --name RimWorld");
    foreach (string line in output.Split('\n')) {
      string trimmed = line.Trim();
      if (trimmed.Length > 0 && ulong.TryParse(trimmed, out _)) {
        Log.InfoTo("Pickle", "xdotool targeting game window {Window}", [trimmed]);
        return trimmed;
      }
    }

    Log.WarnTo("Pickle", "xdotool could not find the RimWorld window; falling back to screen coordinates");
    return null;
  }

  private string Capture(string arguments) {
    try {
      ProcessStartInfo startInfo = new ProcessStartInfo(xdotoolPath, arguments) {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };

      using Process? process = Process.Start(startInfo);
      if (process == null) {
        return $"(failed to start xdotool {arguments})";
      }

      string output = process.StandardOutput.ReadToEnd().Trim();
      process.WaitForExit(TimeoutMs);
      return output;
    } catch (Exception ex) {
      return $"(xdotool {arguments} failed: {ex.Message})";
    }
  }

  // split three ways so a windows user never goes hunting for an xdotool package.
  private string? Probe() {
    if (Environment.OSVersion.Platform != PlatformID.Unix) {
      return "input injection needs X11, so Pickle cannot drive the mouse or keyboard on this platform yet";
    }

    if (string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY"))) {
      return "no DISPLAY, so there is no X server to inject into; run under Xvfb or a real session";
    }

    if (xdotoolPath.Length == 0) {
      return "xdotool is not on PATH; the docker image needs xdotool installed for input injection";
    }

    return null;
  }

  // Never touch startInfo.EnvironmentVariables. Leaving it alone is what makes the
  // child inherit DISPLAY, which XTEST needs to reach the right X server.
  private void Run(string arguments) {
    if (UnavailableReason != null) {
      throw new InvalidOperationException($"{UnavailableReason}. Command: xdotool {arguments}");
    }

    ProcessStartInfo startInfo = new ProcessStartInfo(xdotoolPath, arguments) {
      UseShellExecute = false,
      RedirectStandardError = true,
      CreateNoWindow = true,
    };

    StringBuilder stderr = new StringBuilder();
    using Process process = new Process { StartInfo = startInfo };
    process.ErrorDataReceived += (_, e) => {
      if (e.Data != null) {
        stderr.AppendLine(e.Data);
      }
    };

    process.Start();
    process.BeginErrorReadLine();

    if (!process.WaitForExit(TimeoutMs)) {
      process.Kill();
      throw new InvalidOperationException($"xdotool timed out after {TimeoutMs}ms: xdotool {arguments}");
    }

    process.WaitForExit();

    if (process.ExitCode != 0) {
      throw new InvalidOperationException(
          $"xdotool failed (exit={process.ExitCode}): xdotool {arguments}\n{stderr}");
    }

    if (stderr.Length > 0) {
      Log.WarnTo("Pickle", "xdotool {Arguments} succeeded but wrote to stderr: {Stderr}", [arguments, stderr]);
    }
  }
}
