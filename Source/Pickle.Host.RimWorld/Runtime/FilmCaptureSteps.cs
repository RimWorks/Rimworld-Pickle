using System;
using System.Collections;
using System.IO;
using System.Threading.Tasks;
using RimWorks.Pickle.RimWorldHost;
using UnityEngine;

namespace RimWorks.Pickle.Runtime;

/// <summary>Drives the film's frame capture, so a scenario can end a film while a frame is still being read back.</summary>
[PickleSteps]
public class FilmCaptureSteps {
  /// <summary>Requests one film frame and frees the film's texture in the same frame, before the readback returns.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <returns>A task that completes once the readback has had frames to return.</returns>
  [When("a film ends with a frame still being read back")]
  public async Task EndFilmMidReadback(PickleContext ctx) {
    string filePath = Path.Combine(Path.GetTempPath(), $"pickle-film-readback-{Guid.NewGuid():N}.jpg");
    bool released = false;
    FramePump.Instance.StartCoroutine(RequestThenRelease(filePath, () => released = true));

    await ctx.AssertEventually(() => released, () => "the film frame was never requested");
    await ctx.WaitFrames(30);
    File.Delete(filePath);
  }

  private static IEnumerator RequestThenRelease(string filePath, Action released) {
    yield return new WaitForEndOfFrame();

    FrameCapture.WriteScaledJpeg(filePath, 640);
    FrameCapture.Release();
    released();
  }
}
