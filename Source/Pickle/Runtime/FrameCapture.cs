using System;
using System.IO;
using UnityEngine;
using UnityEngine.Rendering;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>Turns what is on screen into a file. The caller owns the frame timing.</summary>
internal static class FrameCapture {
  private static bool warnedReadback;
  private static bool readOnThisThread;
  private static Texture2D? scratch;

  /// <summary>Writes the screen to a PNG at full resolution.</summary>
  /// <param name="filePath">Where to write the image.</param>
  internal static void WritePng(string filePath) {
    string? dirPath = Path.GetDirectoryName(filePath);
    if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath)) {
      Directory.CreateDirectory(dirPath);
    }

    Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
    try {
      File.WriteAllBytes(filePath, shot.EncodeToPNG());
    } finally {
      UnityEngine.Object.Destroy(shot);
    }
  }

  /// <summary>Writes the screen to a JPEG no wider than <paramref name="maxWidth"/>.</summary>
  /// <param name="filePath">Where to write the image.</param>
  /// <param name="maxWidth">The widest the result may be. Height keeps the aspect ratio.</param>
  internal static void WriteScaledJpeg(string filePath, int maxWidth) {
    // yuv420p rejects an odd width or height
    int width = Mathf.Min(maxWidth, Screen.width);
    width -= width % 2;
    int height = Mathf.RoundToInt(Screen.height * (width / (float)Screen.width));
    height -= height % 2;

    RenderTexture? scaled = null;
    try {
      // the capture the screenshot path already uses
      Texture2D shot = ScreenCapture.CaptureScreenshotAsTexture();
      scaled = RenderTexture.GetTemporary(width, height, 0);

      try {
        Graphics.Blit(shot, scaled);
      } finally {
        UnityEngine.Object.Destroy(shot);
      }

      if (scratch == null || scratch.width != width || scratch.height != height) {
        if (scratch != null) {
          UnityEngine.Object.Destroy(scratch);
        }

        scratch = new Texture2D(width, height, TextureFormat.RGB24, false);
      }

      Read(scaled, scratch, filePath);
    } finally {
      if (scaled != null) {
        RenderTexture.ReleaseTemporary(scaled);
      }
    }
  }

  /// <summary>Frees the texture reused between frames.</summary>
  internal static void Release() {
    if (scratch != null) {
      UnityEngine.Object.Destroy(scratch);
      scratch = null;
    }
  }

  // Software rendering has no async readback, so fall back to pulling the pixels on
  // this thread. Slower, but it is the difference between a film and nothing.
  private static void Read(RenderTexture source, Texture2D scratch, string filePath) {
    if (readOnThisThread) {
      ReadSynchronously(source, scratch, filePath);
      return;
    }

    try {
      Request(source, scratch, filePath);
    } catch (Exception ex) {
      // latched: without it every frame pays for the same missing method
      readOnThisThread = true;
      Log.WarnTo(PickleLog.Channel, ex, "no async readback here, so frames read on this thread");
      ReadSynchronously(source, scratch, filePath);
    }
  }

  private static void Request(RenderTexture source, Texture2D scratch, string filePath) {
    AsyncGPUReadback.Request(source, 0, TextureFormat.RGB24, request => {
      if (request.hasError) {
        WarnReadbackOnce();
        ReadSynchronously(source, scratch, filePath);
        return;
      }

      try {
        scratch.LoadRawTextureData(request.GetData<byte>());
        scratch.Apply(false);
        File.WriteAllBytes(filePath, scratch.EncodeToJPG(75));
      } catch (Exception ex) {
        Log.WarnTo(PickleLog.Channel, ex, $"frame readback failed for {filePath}");
      }
    });
  }

  private static void ReadSynchronously(RenderTexture source, Texture2D scratch, string filePath) {
    RenderTexture? previous = RenderTexture.active;
    try {
      RenderTexture.active = source;
      scratch.ReadPixels(new Rect(0, 0, scratch.width, scratch.height), 0, 0, false);
      scratch.Apply(false);
      File.WriteAllBytes(filePath, scratch.EncodeToJPG(75));
    } catch (Exception ex) {
      Log.WarnTo(PickleLog.Channel, ex, $"frame readback failed for {filePath}");
    } finally {
      RenderTexture.active = previous;
    }
  }

  // software rendering has no async readback, so a filmed run makes nothing. say it once.
  private static void WarnReadbackOnce() {
    if (warnedReadback) {
      return;
    }

    warnedReadback = true;
    Log.WarnTo(PickleLog.Channel,
        "the GPU refused a frame readback, so filming captured nothing. " +
        "This is expected without a real GPU; run with -pickle-max-film-seconds=0 there.");
  }
}
