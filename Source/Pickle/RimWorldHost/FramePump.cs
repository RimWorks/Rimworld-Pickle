using System;
using System.Collections;
using RimWorks.Pickle.Input;
using RimWorks.Pickle.Runtime;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.RimWorldHost;

[StaticConstructorOnStartup]
internal sealed class FramePump : MonoBehaviour {
  public static FramePump Instance { get; } = Create();

  public void CaptureScreenshot(string filePath, Action written) {
    StartCoroutine(CaptureScreenshotCoroutine(filePath, written));
  }

  public void CaptureFrame(string filePath, int maxWidth) {
    StartCoroutine(CaptureFrameCoroutine(filePath, maxWidth));
  }

  private static FramePump Create() {
    GameObject gameObject = new GameObject("PickleFramePump");
    UnityEngine.Object.DontDestroyOnLoad(gameObject);
    return gameObject.AddComponent<FramePump>();
  }

  private static IEnumerator CaptureFrameCoroutine(string filePath, int maxWidth) {
    yield return new WaitForEndOfFrame();

    try {
      FrameCapture.WriteScaledJpeg(filePath, maxWidth);
    } catch (Exception ex) {
      Log.WarnTo(PickleLog.Channel, ex, $"failed to capture frame to {filePath}");
    }
  }

  private static IEnumerator CaptureScreenshotCoroutine(string filePath, Action written) {
    yield return new WaitForEndOfFrame();

    try {
      FrameCapture.WritePng(filePath);
    } catch (Exception ex) {
      Log.WarnTo(PickleLog.Channel, ex, $"failed to capture screenshot to {filePath}");
    }

    written();
  }

#pragma warning disable S2325
  private void Update() {
    if (!PickleDriver.Exists) {
      return;
    }

    PickleDriver.Instance.Update();
    TagStore.BeginFrame();
  }
#pragma warning restore S2325
}
