using System;

namespace RimWorks.Pickle.Hosting;

/// <summary>
/// The host's screen capture, for step screenshots and film frames. The runner picks each file's
/// path, and the host writes the pixels there.
/// </summary>
public interface IScreenCapture {
  /// <summary>Whether the screen is in a fade, so a frame would show the transition instead of the game.</summary>
  bool IsFading { get; }

  /// <summary>Writes the screen to a PNG at full resolution, once the current frame has drawn.</summary>
  /// <param name="filePath">The path to write the image to.</param>
  /// <param name="written">Called once the write is done, or has failed.</param>
  void CaptureScreenshot(string filePath, Action written);

  /// <summary>Writes the screen to a scaled down JPEG, once the current frame has drawn.</summary>
  /// <param name="filePath">The path to write the image to.</param>
  /// <param name="maxWidth">The widest the image may be. Its height keeps the screen's aspect ratio.</param>
  void CaptureFrame(string filePath, int maxWidth);

  /// <summary>Frees what frame capture keeps between frames, once a film is done.</summary>
  void ReleaseFrames();
}
