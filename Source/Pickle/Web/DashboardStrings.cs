using System.Collections.Generic;
using System.Text;
using RimWorks.Pickle.Core;
using Verse;

namespace RimWorks.Pickle.Web;

/// <summary>
/// The browser strings, taken from the same Languages files the in-game UI uses. One
/// source of truth, so a translator touches one file and both surfaces follow.
/// </summary>
public static class DashboardStrings {
  private static readonly string[] Keys = [
    "Pickle_RunAll",
    "Pickle_RunSelected",
    "Pickle_RerunFailed",
    "Pickle_AbortRun",
    "Pickle_SelectAll",
    "Pickle_DeselectAll",
    "Pickle_Aborting",
    "Pickle_WaitingForGame",
    "Pickle_Idle",
    "Pickle_NoFeatures",
    "Pickle_NoReportData",
    "Pickle_SelectScenario",
    "Pickle_Attachments",
    "Pickle_StateAtFailure",
    "Pickle_LogTail",
    "Pickle_Filmstrip",
    "Pickle_Film",
    "Pickle_CloseImage",
    "Pickle_BreakOnFailure",
    "Pickle_IncludeWip",
    "Pickle_ModeWatch",
    "Pickle_ModeFast",
    "Pickle_OutcomePassed",
    "Pickle_OutcomeFailed",
    "Pickle_OutcomeSkipped",
  ];

  /// <summary>Writes the active language as a JSON object of key to text.</summary>
  public static string BuildJson() {
    StringBuilder json = new StringBuilder();
    json.Append('{');

    bool first = true;
    foreach (string key in Keys) {
      if (!first) {
        json.Append(',');
      }

      first = false;
      json.Append(Json.Quote(key)).Append(':').Append(Json.Quote(Translate(key)));
    }

    json.Append('}');
    return json.ToString();
  }

  // A key with no entry translates to the key itself, which would read as
  // "Pickle_RunAll" on a button. Fall back to nothing and let the browser decide.
  private static string Translate(string key) {
    if (!key.CanTranslate()) {
      return string.Empty;
    }

    return key.Translate().ToString();
  }
}
