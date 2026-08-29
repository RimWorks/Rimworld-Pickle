using System;
using System.Threading.Tasks;
using Pickle.Runtime;
using RimWorld;
using UnityEngine;
using Verse;

namespace Pickle.Vanilla;

/// <summary>
/// Camera control. A film is only useful if it points at the thing the scenario is
/// about, and RimWorld has no follow of its own, so Pickle steers one per frame.
/// </summary>
[PickleSteps]
public class CameraSteps {
  // RootSize is half the visible height in cells, so smaller is closer in.
  private const float CloseSize = 12f;
  private const float FarSize = 50f;
  private const float ZoomStep = 8f;

  private static Pawn? followed;
  private static Action? followHook;

  [When("I move the camera to \\({int}, {int}\\)")]
  public async Task MoveTo(PickleContext ctx, int x, int z) {
    Map map = RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    ctx.Require(cell.InBounds(map), $"cell ({x}, {z}) is outside the map, which is {map.Size.x} by {map.Size.z}");

    Find.CameraDriver.JumpToCurrentMapLoc(cell);
    await ctx.WaitFrames(1);
  }

  [When("I move the camera to {string}")]
  public async Task MoveToPawn(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    Find.CameraDriver.JumpToCurrentMapLoc(pawn.Position);
    await ctx.WaitFrames(1);
  }

  // Pans rather than jumps, because a cut looks like a glitch in a recording.
  [When("I pan the camera to \\({int}, {int}\\)")]
  public async Task PanTo(PickleContext ctx, int x, int z) {
    Map map = RequireMap(ctx);
    IntVec3 cell = new IntVec3(x, 0, z);
    ctx.Require(cell.InBounds(map), $"cell ({x}, {z}) is outside the map, which is {map.Size.x} by {map.Size.z}");

    Find.CameraDriver.PanToMapLoc(cell);
    await ctx.WaitFrames(1);
  }

  [When("I follow {string}")]
  public async Task Follow(PickleContext ctx, string nickname) {
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

  [When("I stop following")]
  public void StopFollow(PickleContext ctx) {
    StopFollowing();
  }

  [When("I zoom in")]
  public async Task ZoomIn(PickleContext ctx) {
    await SetSize(ctx, Find.CameraDriver.RootSize - ZoomStep);
  }

  [When("I zoom out")]
  public async Task ZoomOut(PickleContext ctx) {
    await SetSize(ctx, Find.CameraDriver.RootSize + ZoomStep);
  }

  [When("I zoom all the way in")]
  public async Task ZoomAllIn(PickleContext ctx) {
    await SetSize(ctx, CloseSize);
  }

  [When("I zoom all the way out")]
  public async Task ZoomAllOut(PickleContext ctx) {
    await SetSize(ctx, FarSize);
  }

  [Then("the camera is looking at \\({int}, {int}\\)")]
  public void AssertLookingAt(PickleContext ctx, int x, int z) {
    IntVec3 at = Find.CameraDriver.MapPosition;
    ctx.Assert(
        at.x == x && at.z == z,
        $"camera should be at ({x}, {z}); it is at ({at.x}, {at.z})");
  }

  [Then("the camera can see {string}")]
  public void AssertCanSee(PickleContext ctx, string nickname) {
    Pawn pawn = PawnLookup.RequireLiving(nickname);
    ctx.Assert(
        Find.CameraDriver.InViewOf(pawn),
        $"pawn '{nickname}' at {pawn.Position} is outside the view {Find.CameraDriver.CurrentViewRect}");
  }

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
