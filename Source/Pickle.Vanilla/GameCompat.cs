using RimWorld;
using RimWorld.Planet;
using Verse;

namespace RimWorks.Pickle.Vanilla;

// every 1.5-to-1.6 rename the steps hit lives here, so the #if count is one per API and not
// one per call site
internal static class GameCompat {
#if RW_1_5
  public static bool WorldRendered => WorldRendererUtility.WorldRenderedNow;
#else
  public static bool WorldRendered => WorldRendererUtility.WorldRendered;
#endif

  public static FactionDef? DefaultFaction(PawnKindDef kind) {
#if RW_1_5
    return kind.defaultFactionType;
#else
    return kind.defaultFactionDef;
#endif
  }

  public static bool ApparelCovers(Pawn_ApparelTracker apparel, BodyPartGroupDef group) {
#if RW_1_5
    return apparel.BodyPartGroupIsCovered(group);
#else
    return apparel.BodyPartGroupIsCovered(group, null);
#endif
  }

  public static void SetCameraSize(CameraDriver camera, float size) {
#if RW_1_5
    // 1.5 has no size-only setter, so the position argument re-centres on the cell already in view
    camera.SetRootPosAndSize(camera.MapPosition.ToVector3Shifted(), size);
#else
    camera.SetRootSize(size);
#endif
  }

  public static Graphic? PrimaryGraphicOf(PawnRenderNode node) {
#if RW_1_5
    return node.Graphic;
#else
    return node.PrimaryGraphic;
#endif
  }

  public static bool InViewOf(CameraDriver camera, Thing thing) {
#if RW_1_5
    // the body of 1.6's CameraDriver.InViewOf, which 1.5 does not have
    CellRect view = camera.CurrentViewRect.ExpandedBy(1).ClipInsideMap(thing.MapHeld);
    return view.Overlaps(thing.OccupiedDrawRect());
#else
    return camera.InViewOf(thing);
#endif
  }
}
