using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Pickle.Runtime;

namespace Pickle.Evidence;

/// <summary>
/// Samples the screen while a scenario runs, so the report can play back what happened
/// instead of showing one still from the moment it failed.
/// </summary>
public sealed class FilmstripRecorder {
  public const string Tag = "@film";

  // Ten, not thirty: a software renderer cannot read back and encode faster than that.
  // The game rarely hits even this, so the encoder gets the rate measured per scenario.
  public const int TargetFramesPerSecond = 10;

  // 960 wide, not 1920. The readback is async so it does not stall, but the jpeg encode
  // still runs on the main thread and full size frames would show.
  private const int FrameWidth = 960;

  private static readonly List<(string Directory, double Fps)> Recorded = [];

  private readonly Stopwatch clock = new Stopwatch();
  private readonly PickleContext ctx;
  private readonly string featureName;
  private readonly string scenarioName;

  private double nextCaptureAt;
  private int frameIndex;
  private bool capped;

  public FilmstripRecorder(PickleContext ctx, string featureName, string scenarioName) {
    this.ctx = ctx;
    this.featureName = featureName;
    this.scenarioName = scenarioName;
  }

  /// <summary>
  /// Seconds of footage a scenario keeps. A film past this is minutes of encoding, which
  /// once outlived the harness timeout and turned a passing run into exit 124.
  /// </summary>
  public static int MaxSeconds { get; set; } = 60;

  /// <summary>Frame folders captured this run, with the rate each was captured at.</summary>
  public static IReadOnlyList<(string Directory, double Fps)> RecordedFilms => Recorded;

  public void Start() {
    // Zero turns filming off. Software rendering makes frame readback expensive, and a
    // headless run usually wants the report without the video.
    if (MaxSeconds <= 0) {
      return;
    }

    clock.Restart();
    nextCaptureAt = 0.0;
    PickleDriver.Instance.AddFrameHook(OnFrame);
  }

  public void Finish() {
    if (MaxSeconds <= 0) {
      return;
    }

    PickleDriver.Instance.RemoveFrameHook(OnFrame);
    PickleDriver.Instance.ReleaseFrameBuffers();
    clock.Stop();

    if (frameIndex == 0) {
      return;
    }

    double seconds = Math.Min(clock.Elapsed.TotalSeconds, MaxSeconds);
    double fps = seconds > 0.1 ? frameIndex / seconds : TargetFramesPerSecond;

    string dir = ScreenshotCapture.FrameDirectory(featureName, scenarioName);
    if (!Recorded.Any(r => r.Directory == dir)) {
      Recorded.Add((dir, fps));
    }

    // Encoding happens after the run, so the report finds the video by looking beside
    // this frame rather than by an attachment written here.
    ctx.Attach("film-frames", ScreenshotCapture.BuildFramePath(featureName, scenarioName, 0));
  }

  private void OnFrame() {
    if (capped || clock.Elapsed.TotalSeconds >= MaxSeconds) {
      if (!capped) {
        capped = true;
        Verse.Log.Warning(
            $"pickle: '{scenarioName}' passed {MaxSeconds}s, so its film stops there. " +
            "Raise it with -pickle-max-film-seconds.");
      }

      return;
    }

    if (clock.Elapsed.TotalSeconds < nextCaptureAt) {
      return;
    }

    nextCaptureAt = clock.Elapsed.TotalSeconds + (1.0 / TargetFramesPerSecond);
    PickleDriver.Instance.CaptureFrameDetached(
        ScreenshotCapture.BuildFramePath(featureName, scenarioName, frameIndex), FrameWidth);
    frameIndex++;
  }
}
