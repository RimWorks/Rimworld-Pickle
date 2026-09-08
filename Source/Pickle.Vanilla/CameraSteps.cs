using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Runtime;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// Camera control. A film is only useful if it points at the thing the scenario is
/// about, and RimWorld has no follow of its own, so Pickle steers one per frame.
/// </summary>
[PickleSteps]
public static class CameraSteps {
  // RootSize is half the visible height in cells, so smaller is closer in.
  private const float CloseSize = 12f;
  private const float FarSize = 50f;
  private const float ZoomStep = 8f;

  private static Pawn? followed;
  private static Action? followHook;

  /// <summary>Jumps the camera straight to a cell.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I move the camera to \\({int}, {int}\\)")]
  public static async Task MoveTo(PickleContext ctx, int x, int z) {
    Map map = RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    ctx.Require(cell.InBounds(map), $"cell ({x}, {z}) is outside the map, which is {map.Size.x} by {map.Size.z}");

    Find.CameraDriver.JumpToCurrentMapLoc(cell);
    await ctx.WaitFrames(1);
  }

  /// <summary>Jumps the camera straight to a pawn's current position.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to jump to.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I move the camera to {string}")]
  public static async Task MoveToPawn(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Find.CameraDriver.JumpToCurrentMapLoc(pawn.Position);
    await ctx.WaitFrames(1);
  }

  /// <summary>Pans the camera to a cell instead of cutting to it.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="x">The cell's x coordinate.</param>
  /// <param name="z">The cell's z coordinate.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  // Pans rather than jumps, because a cut looks like a glitch in a recording.
  [When("I pan the camera to \\({int}, {int}\\)")]
  public static async Task PanTo(PickleContext ctx, int x, int z) {
    Map map = RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    ctx.Require(cell.InBounds(map), $"cell ({x}, {z}) is outside the map, which is {map.Size.x} by {map.Size.z}");

    Find.CameraDriver.PanToMapLoc(cell);
    await ctx.WaitFrames(1);
  }

  /// <summary>Registers a per-frame hook that keeps the camera on a pawn until it stops being followed.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn to follow.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I follow {string}")]
  public static async Task Follow(PickleContext ctx, string nickname) {
    StopFollowing();

    Pawn pawn = PawnLookup.RequireLiving(nickname);
    followed = pawn;
    followHook = () => {
      if (followed is { Spawned: true }) {
        Find.CameraDriver.JumpToCurrentMapLoc(followed.DrawPos);
      }
    };

    PickleDriver.Instance.AddFrameHook(followHook);
    await ctx.WaitFrames(1);
  }

  /// <summary>Stops the camera from following whatever pawn it was locked onto.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [When("I stop following")]
  public static void StopFollow(PickleContext ctx) {
    StopFollowing();
  }

  /// <summary>Zooms in one step.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I zoom in")]
  public static async Task ZoomIn(PickleContext ctx) {
    await SetSize(ctx, Find.CameraDriver.RootSize - ZoomStep);
  }

  /// <summary>Zooms out one step.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I zoom out")]
  public static async Task ZoomOut(PickleContext ctx) {
    await SetSize(ctx, Find.CameraDriver.RootSize + ZoomStep);
  }

  /// <summary>Zooms all the way in.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I zoom all the way in")]
  public static async Task ZoomAllIn(PickleContext ctx) {
    await SetSize(ctx, CloseSize);
  }

  /// <summary>Zooms all the way out.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes when the step finishes. A failed assertion faults it.</returns>
  [When("I zoom all the way out")]
  public static async Task ZoomAllOut(PickleContext ctx) {
    await SetSize(ctx, FarSize);
  }

  /// <summary>Asserts the camera's map position.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="x">The expected x coordinate.</param>
  /// <param name="z">The expected z coordinate.</param>
  [Then("the camera is looking at \\({int}, {int}\\)")]
  public static void AssertLookingAt(PickleContext ctx, int x, int z) {
    IntVec3 at = Find.CameraDriver.MapPosition;
    ctx.Assert(
        at.x == x && at.z == z,
        $"camera should be at ({x}, {z}); it is at ({at.x}, {at.z})");
  }

  /// <summary>Asserts a pawn is inside the camera's current view rect.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="nickname">The pawn expected to be visible.</param>
  [Then("the camera can see {string}")]
  public static void AssertCanSee(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Assert(
        Find.CameraDriver.InViewOf(pawn),
        $"pawn '{nickname}' at {pawn.Position} is outside the view {Find.CameraDriver.CurrentViewRect}");
  }

  /// <summary>Removes the follow hook, if one is active. Safe to call when nothing is being followed.</summary>
  internal static void StopFollowing() {
    if (followHook != null && PickleDriver.Exists) {
      PickleDriver.Instance.RemoveFrameHook(followHook);
    }

    followHook = null;
    followed = null;
  }

  private static async Task SetSize(PickleContext ctx, float size) {
    Find.CameraDriver.SetRootSize(Mathf.Clamp(size, CloseSize, FarSize));
    await ctx.WaitFrames(1);
  }

  private static Map RequireMap(PickleContext ctx) {
    Map? map = Find.CurrentMap;
    ctx.Require(map != null, "no current map is loaded; load a save first with 'the save ... is loaded'");
    return map!;
  }
}
