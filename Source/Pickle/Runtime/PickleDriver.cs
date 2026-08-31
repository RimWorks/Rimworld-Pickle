using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using Pickle.Input;
using UnityEngine;
using UnityEngine.Rendering;
using Verse;

namespace Pickle.Runtime;

/// <summary>
/// Runs the async step pump. Update resolves waits and invokes continuations directly,
/// so await always resumes inline on the main thread, which step code needs.
/// </summary>
public class PickleDriver : MonoBehaviour {
  private static PickleDriver? instance;
  private static bool warnedFrameReadback;

  private readonly ConcurrentQueue<Action> mainThreadQueue = new();

  // Guards pendingWaits: registration can come from a background thread while Update()
  // scans on the main thread. Continuations are always invoked outside this lock.
  private readonly object waitsGate = new object();
  private readonly List<PendingWait> pendingWaits = [];
  private int frameCounter;

  private RenderTexture? frameTarget;
  private Texture2D? frameScratch;
  private int mainThreadId;

  public static bool Exists => instance != null;

  public static PickleDriver Instance {
    get {
      EnsureExists();
      return instance!;
    }
  }

  // Runs once per rendered frame. Multicast on purpose: the filmstrip samples here and
  // a camera follow steers here, and both can be live at the same time.
  public Action? FrameHook { get; set; }

  public static void EnsureExists() {
    if (instance != null) {
      return;
    }

    GameObject gameObject = new GameObject("PickleDriver");
    UnityEngine.Object.DontDestroyOnLoad(gameObject);
    instance = gameObject.AddComponent<PickleDriver>();
  }

  public static void Post(Action action) {
    Instance.mainThreadQueue.Enqueue(action);
  }

  public void AddFrameHook(Action hook) {
    FrameHook += hook;
  }

  public void RemoveFrameHook(Action hook) {
    FrameHook -= hook;
  }

  public PickleWait WaitTicks(int n, object? scope = null) {
    if (Current.Game == null) {
      return new PickleWait(new InvalidOperationException(
          "PickleDriver.WaitTicks: no game is running (Current.Game is null); this wait would never complete."));
    }

    PendingWait wait = new PendingWait(PendingWaitKind.Ticks) {
      TargetTicksGame = Find.TickManager.TicksGame + n,
      Scope = scope,
    };
    lock (waitsGate) {
      pendingWaits.Add(wait);
    }
    return new PickleWait(wait);
  }

  public PickleWait WaitFrames(int n, object? scope = null) {
    PendingWait wait = new PendingWait(PendingWaitKind.Frames) {
      TargetFrame = frameCounter + n,
      Scope = scope,
    };
    lock (waitsGate) {
      pendingWaits.Add(wait);
    }
    return new PickleWait(wait);
  }

  public PickleWait WaitUntil(Func<bool> cond, float timeoutSeconds, object? scope = null) {
    PendingWait wait = new PendingWait(PendingWaitKind.Until) {
      Condition = cond,
      DeadlineRealtime = Time.realtimeSinceStartup + timeoutSeconds,
      TimeoutSeconds = timeoutSeconds,
      Scope = scope,
    };
    lock (waitsGate) {
      pendingWaits.Add(wait);
    }
    return new PickleWait(wait);
  }

  public PickleWait CaptureScreenshot(string filePath) {
    PendingWait wait = new PendingWait(PendingWaitKind.Frames) {
      TargetFrame = frameCounter,
      Scope = null,
    };
    lock (waitsGate) {
      pendingWaits.Add(wait);
    }

    StartCoroutine(CaptureScreenshotCoroutine(filePath, wait));
    return new PickleWait(wait);
  }

  // no PendingWait: the filmstrip fires frames as steps finish and nothing awaits them,
  // so registering waits here would leave entries for FaultAllPending to trip over
  public void CaptureFrameDetached(string filePath, int maxWidth) {
    StartCoroutine(CaptureFrameCoroutine(filePath, maxWidth));
  }

  public void ReleaseFrameBuffers() {
    if (frameTarget != null) {
      RenderTexture.ReleaseTemporary(frameTarget);
      frameTarget = null;
    }

    if (frameScratch != null) {
      UnityEngine.Object.Destroy(frameScratch);
      frameScratch = null;
    }
  }

  public void FaultScope(object scope, Exception exception) {
    List<PendingWait> faulted = [];
    lock (waitsGate) {
      for (int i = pendingWaits.Count - 1; i >= 0; i--) {
        PendingWait wait = pendingWaits[i];
        if (!ReferenceEquals(wait.Scope, scope)) {
          continue;
        }

        pendingWaits.RemoveAt(i);
        faulted.Add(wait);
      }
    }

    foreach (PendingWait wait in faulted) {
      Resolve(wait, exception);
    }
  }

  public void FaultAllPending(Exception exception) {
    List<PendingWait> faulted = [];
    lock (waitsGate) {
      faulted.AddRange(pendingWaits);
      pendingWaits.Clear();
    }

    foreach (PendingWait wait in faulted) {
      Resolve(wait, exception);
    }
  }

  // CaptureScreenshotIntoRenderTexture keeps the frame on the GPU and AsyncGPUReadback
  // pulls it back without stalling, which is what makes filming a live run possible.
  // ReadPixels blocks until the GPU catches up and cannot keep that rate.
  // Software rendering has no working async readback, so a filmed run silently produced
  // nothing. Say it once rather than per frame.
  private static void WarnFrameReadbackOnce() {
    if (warnedFrameReadback) {
      return;
    }

    warnedFrameReadback = true;
    Log.Warning(
        "pickle: the GPU refused a frame readback, so filming captured nothing. " +
        "This is expected without a real GPU; run with -pickle-max-film-seconds=0 there.");
  }

  private static void ReadFrameSynchronously(RenderTexture source, Texture2D scratch, string filePath) {
    RenderTexture? previous = RenderTexture.active;
    try {
      RenderTexture.active = source;
      scratch.ReadPixels(new Rect(0, 0, scratch.width, scratch.height), 0, 0, false);
      scratch.Apply(false);
      File.WriteAllBytes(filePath, scratch.EncodeToJPG(75));
    } catch (Exception ex) {
      Log.Warning($"pickle: frame readback failed for {filePath}: {ex.Message}");
    } finally {
      RenderTexture.active = previous;
    }
  }

  private System.Collections.IEnumerator CaptureFrameCoroutine(string filePath, int maxWidth) {
    yield return new WaitForEndOfFrame();

    RenderTexture? scaled = null;
    try {
      int width = Mathf.Min(maxWidth, Screen.width);

      // yuv420p rejects an odd width or height
      width -= width % 2;
      int height = Mathf.RoundToInt(Screen.height * (width / (float)Screen.width));
      height -= height % 2;

      if (frameTarget == null || frameTarget.width != Screen.width || frameTarget.height != Screen.height) {
        ReleaseFrameBuffers();
        frameTarget = RenderTexture.GetTemporary(Screen.width, Screen.height, 0);
      }

      ScreenCapture.CaptureScreenshotIntoRenderTexture(frameTarget);

      scaled = RenderTexture.GetTemporary(width, height, 0);
      Graphics.Blit(frameTarget, scaled);

      if (frameScratch == null || frameScratch.width != width || frameScratch.height != height) {
        if (frameScratch != null) {
          UnityEngine.Object.Destroy(frameScratch);
        }

        frameScratch = new Texture2D(width, height, TextureFormat.RGB24, false);
      }

      Texture2D scratch = frameScratch;

      // Software rendering has no async readback, so fall back to pulling the pixels on
      // this thread. Slower, but it is the difference between a film and nothing.
      if (SystemInfo.supportsAsyncGPUReadback) {
        RenderTexture source = scaled;
        AsyncGPUReadback.Request(scaled, 0, TextureFormat.RGB24, request => {
          if (request.hasError) {
            WarnFrameReadbackOnce();
            ReadFrameSynchronously(source, scratch, filePath);
            return;
          }

          try {
            scratch.LoadRawTextureData(request.GetData<byte>());
            scratch.Apply(false);
            File.WriteAllBytes(filePath, scratch.EncodeToJPG(75));
          } catch (Exception ex) {
            Log.Warning($"pickle: frame readback failed for {filePath}: {ex.Message}");
          }
        });
      } else {
        ReadFrameSynchronously(scaled, scratch, filePath);
      }
    } catch (Exception ex) {
      Log.Warning($"pickle: failed to capture frame to {filePath}: {ex.Message}");
    } finally {
      if (scaled != null) {
        RenderTexture.ReleaseTemporary(scaled);
      }
    }
  }

  private System.Collections.IEnumerator CaptureScreenshotCoroutine(string filePath, PendingWait? wait) {
    yield return new WaitForEndOfFrame();

    try {
      string? dirPath = Path.GetDirectoryName(filePath);
      if (!string.IsNullOrEmpty(dirPath) && !Directory.Exists(dirPath)) {
        Directory.CreateDirectory(dirPath);
      }

      Texture2D screenshot = ScreenCapture.CaptureScreenshotAsTexture();
      byte[] pngData = screenshot.EncodeToPNG();
      UnityEngine.Object.Destroy(screenshot);

      File.WriteAllBytes(filePath, pngData);
    } catch (Exception ex) {
      Log.Warning($"pickle: failed to capture screenshot to {filePath}: {ex.Message}");
    }

    if (wait == null) {
      yield break;
    }

    lock (waitsGate) {
      if (pendingWaits.Remove(wait)) {
        Resolve(wait, null);
      }
    }
  }

  private void Awake() {
    mainThreadId = Thread.CurrentThread.ManagedThreadId;
  }

  private void Update() {
    while (mainThreadQueue.TryDequeue(out Action action)) {
      try {
        action();
      } catch (Exception ex) {
        Log.Error($"pickle: posted action threw: {ex}");
      }
    }

    frameCounter++;

    try {
      FrameHook?.Invoke();
    } catch (Exception ex) {
      Log.Error($"pickle: frame hook threw: {ex}");
    }

    ScanWaits();

    // BeginFrame() after ScanWaits: steps during Update(N) see tags from OnGUI(N-1), then we clear for OnGUI(N).
    TagStore.BeginFrame();
  }

  private void ScanWaits() {
    List<PendingWait> snapshot;
    lock (waitsGate) {
      if (pendingWaits.Count == 0) {
        return;
      }

      snapshot = [.. pendingWaits];
    }

    foreach (PendingWait wait in snapshot) {
      bool done = Evaluate(wait, out Exception? fault);
      if (!done) {
        continue;
      }

      bool claimed;
      lock (waitsGate) {
        claimed = pendingWaits.Remove(wait);
      }

      if (claimed) {
        Resolve(wait, fault);
      }
    }
  }

  private void Resolve(PendingWait wait, Exception? fault) {
    Action? continuation = wait.Resolve(fault);
    if (continuation == null) {
      return;
    }

    // Regression alarm for the bug this type exists to fix: resuming step
    // code off the main thread would crash on its next Unity/Verse call.
    if (Thread.CurrentThread.ManagedThreadId != mainThreadId) {
      Log.Error($"pickle: PickleDriver continuation invoked off the main thread (thread={Thread.CurrentThread.ManagedThreadId}, expected={mainThreadId})");
    }

    continuation();
  }

  private bool Evaluate(PendingWait wait, out Exception? fault) {
    fault = null;
    switch (wait.Kind) {
      case PendingWaitKind.Ticks:
        if (Current.Game == null) {
          fault = new InvalidOperationException("PickleDriver.WaitTicks: the game ended while waiting for ticks.");
          return true;
        }

        return Find.TickManager.TicksGame >= wait.TargetTicksGame;
      case PendingWaitKind.Frames:
        return frameCounter >= wait.TargetFrame;
      case PendingWaitKind.Until:
        try {
          if (wait.Condition!.Invoke()) {
            return true;
          }
        } catch (Exception ex) {
          fault = ex;
          return true;
        }

        if (Time.realtimeSinceStartup <= wait.DeadlineRealtime) {
          return false;
        }

        fault = new TimeoutException($"PickleDriver.WaitUntil timed out after {wait.TimeoutSeconds:0.##}s");
        return true;
      default:
        return false;
    }
  }
}
