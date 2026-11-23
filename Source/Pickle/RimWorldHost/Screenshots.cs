using System;
using RimWorks.Pickle.Hosting;
using RimWorks.Pickle.Runtime;
using Verse;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class Screenshots : IScreenCapture {
  public bool IsFading => ScreenFader.IsFading();

  public void CaptureScreenshot(string filePath, Action written) {
    FramePump.Instance.CaptureScreenshot(filePath, written);
  }

  public void CaptureFrame(string filePath, int maxWidth) {
    FramePump.Instance.CaptureFrame(filePath, maxWidth);
  }

  public void ReleaseFrames() {
    FrameCapture.Release();
  }
}
