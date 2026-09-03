using System.Reflection;
using RimWorks.Pickle.Runtime;
using RimWorld;
using UnityEngine;
using Verse;

namespace RimWorks.Pickle.UI;

/// <summary>
/// A "Pickle Runner" button under the language button on the main menu, so the runner is
/// reachable without turning on dev mode and hunting through the debug actions.
/// </summary>
public static class MainMenuRunnerButton {
  private const float ColumnX = 187f;
  private const float ColumnWidth = 145f;
  private const float ButtonHeight = 50f;
  private const float LanguageButtonBottom = 60f;

  // MainMenuDrawer parks the bottom of the web-links column here and puts the language
  // button 10px under it. Private, and the only honest way to sit below that button.
  private static readonly FieldInfo? WebBackgroundYMax =
      typeof(MainMenuDrawer).GetField("webBackgroundYMax", BindingFlags.NonPublic | BindingFlags.Static);

  /// <param name="rect">The rect DoMainMenuControls was given, in screen space.</param>
  public static void Draw(Rect rect) {
    // Same gate the language button uses: in-game the runner is on the debug menu instead.
    if (Current.ProgramState != ProgramState.Entry || WebBackgroundYMax == null) {
      return;
    }

    float columnBottom = (float)WebBackgroundYMax.GetValue(null);
    Rect buttonRect = new Rect(
        rect.x + ColumnX,
        rect.y + columnBottom + LanguageButtonBottom + 4f,
        ColumnWidth,
        ButtonHeight);

    if (Widgets.ButtonText(buttonRect, "Pickle_RunnerMenuButton".Translate())) {
      PickleDriver.EnsureExists();
      Find.WindowStack.Add(RunnerWindow.Instance);
    }
  }
}
