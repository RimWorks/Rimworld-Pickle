using RimWorks.Pickle.Core.Ui;
using UnityEngine;

namespace RimWorks.Pickle.Input;

internal static class InteractionRequest {
  private static volatile bool clickArmed;
  private static volatile bool clickFired;
  private static volatile bool hoverArmed;
  private static Rect clickTarget;
  private static Rect hoverTarget;

  public static bool ClickFired => clickFired;

  public static void ArmClick(Rect screenRect) {
    clickTarget = screenRect;
    clickFired = false;
    clickArmed = true;
  }

  public static void SetHover(Rect screenRect) {
    hoverTarget = screenRect;
    hoverArmed = true;
  }

  public static void Clear() {
    clickArmed = false;
    clickFired = false;
    hoverArmed = false;
  }

  public static bool TakeClick(Rect rect) {
    if (!clickArmed || !MatchesInScreenSpace(rect, clickTarget)) {
      return false;
    }

    clickArmed = false;
    clickFired = true;
    return true;
  }

  public static bool IsHovered(Rect rect) {
    return hoverArmed && MatchesInScreenSpace(rect, hoverTarget);
  }

  private static bool MatchesInScreenSpace(Rect guiRect, Rect screenTarget) {
    return RectMatch.Same(Edges(GUIUtility.GUIToScreenRect(guiRect)), Edges(screenTarget));
  }

  private static (float X, float Y, float Width, float Height) Edges(Rect rect) {
    return (rect.x, rect.y, rect.width, rect.height);
  }
}
