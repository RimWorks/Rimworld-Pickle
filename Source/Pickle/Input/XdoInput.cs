using System;
using System.Diagnostics;
using System.Text;
using UnityEngine;
using Verse;

namespace Pickle.Input;

/// <summary>
/// XTEST input via xdotool is indistinguishable from a human mouse at the X11 level, so
/// it reaches native dispatch. The pointer really moves; windowed mode isolates it.
/// </summary>
public static class XdoInput {
  private const int TimeoutMs = 2000;

  private static readonly string XdotoolPath = ResolveXdotool();

  private static bool? available;
  private static string? gameWindowId;

  public static bool Available => available ??= Probe();

  public static void MoveTo(Vector2 guiPoint) {
    Vector2 target = ToScreen(guiPoint);
    Run($"mousemove {WindowArg()} --sync {(int)target.x} {(int)target.y}");
  }

  public static void Click(Vector2 guiPoint, int button = 1) {
    Vector2 target = ToScreen(guiPoint);
    Run($"mousemove {WindowArg()} --sync {(int)target.x} {(int)target.y} click {button}");
  }

  // The one place GUI space becomes X11 screen space. Both are top-left, so this only
  // applies UIScale; add an offset here if clicks land wrong.
  public static Vector2 ToScreen(Vector2 guiPoint) {
    float scale = Prefs.UIScale;
    return new Vector2(guiPoint.x * scale, guiPoint.y * scale);
  }

  // Callers report this alongside the intended target when a click fails to land: it
  // separates a coordinate mapping bug from a click that went to the right place.
  public static string GetMouseLocation() {
    return Capture("getmouselocation");
  }

  // Coordinates are relative to the game window, not the X screen. They match under
  // Xvfb only because the window sits at the origin.
  private static string WindowArg() {
    gameWindowId ??= FindGameWindow();
    return gameWindowId == null ? string.Empty : $"--window {gameWindowId}";
  }

  private static string? FindGameWindow() {
    string output = Capture("search --name RimWorld");
    foreach (string line in output.Split('\n')) {
      string trimmed = line.Trim();
      if (trimmed.Length > 0 && ulong.TryParse(trimmed, out _)) {
        Log.Message($"pickle: xdotool targeting game window {trimmed}");
        return trimmed;
      }
    }

    Log.Warning("pickle: xdotool could not find the RimWorld window; falling back to screen coordinates");
    return null;
  }

  private static string Capture(string arguments) {
    try {
      ProcessStartInfo startInfo = new ProcessStartInfo(XdotoolPath, arguments) {
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

  private static bool Probe() {
    return XdotoolPath.Length > 0;
  }

  // Never touch startInfo.EnvironmentVariables. Leaving it alone is what makes the
  // child inherit DISPLAY, which XTEST needs to reach the right X server.
  private static void Run(string arguments) {
    if (!Available) {
      throw new InvalidOperationException(
          $"xdotool is not on PATH; the docker image needs xdotool installed for click injection. Command: xdotool {arguments}");
    }

    ProcessStartInfo startInfo = new ProcessStartInfo(XdotoolPath, arguments) {
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
      Log.Warning($"pickle: xdotool {arguments} succeeded but wrote to stderr: {stderr}");
    }
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
}
