using System;
using System.Diagnostics;
using System.IO;
using Verse;

namespace RimWorks.Pickle.Evidence;

/// <summary>
/// Turns a folder of filmstrip frames into a webm, when ffmpeg is on the PATH. Nothing
/// in Unity can encode video, so this is the only route to a file a browser will play.
/// </summary>
public static class FilmEncoder {
  private const int TimeoutMs = 60000;

  private static string? cachedFfmpeg;
  private static bool probed;

  public static bool Available => ResolveFfmpeg().Length > 0;

  /// <summary>
  /// Returns the webm path, or null when ffmpeg is missing or the encode failed. The
  /// strip stays in the report either way, so a missing encoder costs nothing.
  /// </summary>
  public static string? TryEncode(string frameDirectory, double framesPerSecond) {
    string ffmpeg = ResolveFfmpeg();
    if (ffmpeg.Length == 0) {
      Log.Warning(
          $"pickle: no ffmpeg on PATH, so {frameDirectory} keeps its frames and gets no video.");
      return null;
    }

    string output = Path.Combine(frameDirectory, "film.webm");

    try {
      ProcessStartInfo startInfo = new ProcessStartInfo(
          ffmpeg,
          $"-y -framerate {framesPerSecond.ToString("0.###", System.Globalization.CultureInfo.InvariantCulture)} -i \"{Path.Combine(frameDirectory, "%04d.jpg")}\" " +
          "-c:v libvpx-vp9 -pix_fmt yuv420p -b:v 0 -crf 38 -row-mt 1 " +
          $"\"{output}\"") {
        UseShellExecute = false,
        RedirectStandardOutput = true,
        RedirectStandardError = true,
        CreateNoWindow = true,
      };

      using Process? process = Process.Start(startInfo);
      if (process == null) {
        return null;
      }

      process.StandardOutput.ReadToEnd();
      string errors = process.StandardError.ReadToEnd();

      if (!process.WaitForExit(TimeoutMs) || process.ExitCode != 0) {
        Log.Warning($"pickle: ffmpeg could not encode {frameDirectory}: {Tail(errors)}");
        return null;
      }

      if (!File.Exists(output)) {
        return null;
      }

      PurgeFrames(frameDirectory);
      return output;
    } catch (Exception ex) {
      Log.Warning($"pickle: ffmpeg failed for {frameDirectory}: {ex.Message}");
      return null;
    }
  }

  // Only after a webm exists. A failed or missing encode leaves the frames alone, so
  // the report still has a strip to fall back on.
  private static void PurgeFrames(string frameDirectory) {
    try {
      foreach (string frame in Directory.GetFiles(frameDirectory, "*.jpg")) {
        File.Delete(frame);
      }
    } catch (Exception ex) {
      Log.Warning($"pickle: kept frames in {frameDirectory}: {ex.Message}");
    }
  }

  private static string Tail(string text) {
    string[] lines = text.Split('\n');
    return lines.Length == 0 ? string.Empty : lines[lines.Length - 1].Trim();
  }

  // Walks PATH rather than passing a bare name to the process launcher, matching how
  // XdoInput resolves its binary.
  private static string ResolveFfmpeg() {
    if (probed) {
      return cachedFfmpeg ?? string.Empty;
    }

    probed = true;
    string search = Environment.GetEnvironmentVariable("PATH") ?? string.Empty;
    foreach (string dir in search.Split(Path.PathSeparator)) {
      if (dir.Length == 0) {
        continue;
      }

      foreach (string name in new[] { "ffmpeg", "ffmpeg.exe" }) {
        string candidate = Path.Combine(dir, name);
        if (File.Exists(candidate)) {
          cachedFfmpeg = candidate;
          return candidate;
        }
      }
    }

    cachedFfmpeg = string.Empty;
    return string.Empty;
  }
}
