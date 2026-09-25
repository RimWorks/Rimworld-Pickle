using System.Reflection;
using System.Threading.Tasks;
using RimWorks.Pickle.Runtime;
using RimWorld.Planet;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>
/// Planet camera control. <see cref="CameraSteps"/> drives the colony camera, which moves
/// nothing while the world map is up, so the planet view gets its own steps.
/// </summary>
[PickleSteps]
public static class WorldCameraSteps {
  private const float MaxAltitude = 1100f;
  private const float ZoomStep = 120f;
  private const float SettleSeconds = 5f;
  private const float PlanetSeconds = 60f;
  private const float AtAltitude = 0.5f;

  private static readonly FieldInfo? DesiredAltitude =
      typeof(WorldCameraDriver).GetField("desiredAltitude", BindingFlags.Instance | BindingFlags.NonPublic);

  /// <summary>Opens the planet view.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes once the world is on screen.</returns>
  // Showing the world queues a "GeneratingPlanet" long event, and WorldCameraDriver.Update
  // returns on its first line for as long as one runs.
  [When("I open the world view", TimeoutSeconds = PlanetSeconds + 5f)]
  public static async Task OpenWorldView(PickleContext ctx) {
    ctx.Require(CameraJumper.TryShowWorld(), "the world view would not open; the game has to be in play");
    await ctx.WaitUntil(
        () => WorldRendererUtility.WorldRendered && !LongEventHandler.AnyEventNowOrWaiting,
        PlanetSeconds);
  }

  /// <summary>Closes the planet view and goes back to the colony.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes once the world is off screen.</returns>
  [When("I close the world view")]
  public static async Task CloseWorldView(PickleContext ctx) {
    CameraJumper.TryHideWorld();
    await ctx.WaitUntil(() => !WorldRendererUtility.WorldRendered, SettleSeconds);
  }

  /// <summary>Jumps the planet camera to a tile and selects that tile's layer.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="tile">The tile id to jump to.</param>
  /// <returns>A task that completes once the camera stops moving.</returns>
  [When("I move the world camera to tile {int}")]
  public static async Task MoveToTile(PickleContext ctx, int tile) {
    WorldCameraDriver camera = RequireWorldCamera(ctx);
    ctx.Require(
        tile >= 0 && tile < Find.WorldGrid.TilesCount,
        $"tile {tile} is off the planet, which has {Find.WorldGrid.TilesCount} tiles");

    camera.JumpTo(tile);
    await Settle(ctx);
  }

  /// <summary>Zooms the planet camera in one step.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes once the camera stops moving.</returns>
  [When("I zoom the world camera in")]
  public static async Task ZoomIn(PickleContext ctx) {
    await SetAltitude(ctx, RequireWorldCamera(ctx).altitude - ZoomStep);
  }

  /// <summary>Zooms the planet camera out one step.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes once the camera stops moving.</returns>
  [When("I zoom the world camera out")]
  public static async Task ZoomOut(PickleContext ctx) {
    await SetAltitude(ctx, RequireWorldCamera(ctx).altitude + ZoomStep);
  }

  /// <summary>Zooms the planet camera all the way in.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes once the camera stops moving.</returns>
  [When("I zoom the world camera all the way in")]
  public static async Task ZoomAllIn(PickleContext ctx) {
    await SetAltitude(ctx, WorldCameraDriver.MinAltitude);
  }

  /// <summary>Zooms the planet camera all the way out.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes once the camera stops moving.</returns>
  [When("I zoom the world camera all the way out")]
  public static async Task ZoomAllOut(PickleContext ctx) {
    await SetAltitude(ctx, MaxAltitude);
  }

  /// <summary>Turns the planet so north is up.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <returns>A task that completes once the camera stops moving.</returns>
  [When("I turn the world camera north up")]
  public static async Task NorthUp(PickleContext ctx) {
    RequireWorldCamera(ctx).RotateSoNorthIsUp(interpolate: true);
    await Settle(ctx);
  }

  /// <summary>Asserts the planet camera is centered on a tile.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="tile">The tile id the camera should be looking at.</param>
  [Then("the world camera is looking at tile {int}")]
  public static void AssertLookingAtTile(PickleContext ctx, int tile) {
    int at = CenteredTile(RequireWorldCamera(ctx));
    ctx.Assert(at == tile, $"the world camera is looking at tile {at}, not {tile}");
  }

  /// <summary>Asserts the planet camera sits at one end of its zoom range.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  /// <param name="end">Either <c>in</c> or <c>out</c>.</param>
  [Then("the world camera is zoomed all the way {word}")]
  public static void AssertZoomedAllTheWay(PickleContext ctx, string end) {
    WorldCameraDriver camera = RequireWorldCamera(ctx);
    ctx.Require(end == "in" || end == "out", $"'{end}' is not a zoom end; use 'in' or 'out'");

    float want = end == "in" ? 0f : 1f;
    ctx.Assert(
        Mathf.Abs(camera.AltitudePercent - want) < 0.02f,
        $"the world camera is at {camera.AltitudePercent:P0} of its zoom range, not all the way {end}");
  }

  private static int CenteredTile(WorldCameraDriver camera) {
    Vector3 looking = camera.CurrentlyLookingAtPointOnSphere.normalized;
    WorldGrid grid = Find.WorldGrid;

    int best = 0;
    float bestDot = float.NegativeInfinity;
    for (int tile = 0; tile < grid.TilesCount; tile++) {
      float dot = Vector3.Dot(grid.GetTileCenter(tile).normalized, looking);
      if (dot > bestDot) {
        bestDot = dot;
        best = tile;
      }
    }

    return best;
  }

  // Waits for the camera to reach the altitude rather than to stop changing: a camera the game
  // is not updating at all reads as stopped, which is how a zoom nobody applied looked settled.
  private static PickleWait SetAltitude(PickleContext ctx, float altitude) {
    WorldCameraDriver camera = RequireWorldCamera(ctx);
    ctx.Require(
        DesiredAltitude != null,
        "RimWorld renamed WorldCameraDriver.desiredAltitude, so Pickle cannot zoom the world camera");

    float target = Mathf.Clamp(altitude, WorldCameraDriver.MinAltitude, MaxAltitude);
    DesiredAltitude!.SetValue(camera, target);

    return ctx.WaitUntil(() => Mathf.Abs(camera.altitude - target) < AtAltitude, SettleSeconds);
  }

  private static PickleWait Settle(PickleContext ctx) {
    WorldCameraDriver camera = Find.WorldCameraDriver;
    float lastAltitude = float.NaN;
    Quaternion lastRotation = camera.sphereRotation;

    return ctx.WaitUntil(
        () => {
          bool still = Mathf.Abs(camera.altitude - lastAltitude) < 0.01f
              && Quaternion.Angle(camera.sphereRotation, lastRotation) < 0.01f;

          lastAltitude = camera.altitude;
          lastRotation = camera.sphereRotation;
          return still;
        },
        SettleSeconds);
  }

  private static WorldCameraDriver RequireWorldCamera(PickleContext ctx) {
    ctx.Require(
        WorldRendererUtility.WorldRendered,
        "the world map is not on screen, so these steps move a camera nobody is looking at; "
            + "open the world view, or use a colony camera step");

    return Find.WorldCameraDriver;
  }
}
