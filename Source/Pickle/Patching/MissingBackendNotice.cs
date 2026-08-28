using Verse;

namespace Pickle.Patching;

/// <summary>
/// Warns when Pickle loaded with no patching library. modDependencies is a flat AND
/// list, so "Harmony or Concord" cannot be declared there.
/// </summary>
public static class MissingBackendNotice {
  private const string HarmonyUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=2009463077";
  private const string ConcordUrl = "https://steamcommunity.com/sharedfiles/filedetails/?id=3758333473";

  private static bool shown;

  /// <summary>
  /// Dev mode only. Pickle is a test runner, so anyone who can act on this already
  /// has dev mode on, and a player who left the mod enabled is not interrupted.
  /// </summary>
  public static void ShowIfDevMode() {
    if (shown || !Prefs.DevMode) {
      return;
    }

    shown = true;

    string text =
        "Pickle has no patching library, so it cannot run any tests.\n\n" +
        "Enable Harmony or Concord, then restart RimWorld. Pickle uses Concord when both are active.\n\n" +
        $"Harmony: {HarmonyUrl}\n" +
        $"Concord: {ConcordUrl}";

    // Without a cancelAction the dialog leaves closeOnCancel false, so Escape does
    // nothing. Vanilla's own CreateConfirmation passes an empty one for that reason.
    Find.WindowStack.Add(new Dialog_MessageBox(
        text, "Close".Translate(), null, null, null, "Pickle",
        buttonADestructive: false, acceptAction: null, cancelAction: () => { }));
  }
}
