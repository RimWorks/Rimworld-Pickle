using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Verse;

namespace RimWorks.Pickle.Vanilla;

/// <summary>Switching the active language, for a suite that has a pass to run in each one.</summary>
[PickleSteps]
public class LanguageSteps {
  /// <summary>Switches the active language and waits for the reload to finish.</summary>
  /// <remarks>
  /// <c>LanguageDatabase.SelectLanguage</c> does not finish inside the call: the game reloads the
  /// language data, and until it does there is no active language at all - the log fills with
  /// "No active language! Cannot translate from key" while it lasts. A scenario asserting straight
  /// after the call reads the old language on a slow machine, so the wait belongs here rather than
  /// in every suite that needs it.
  /// </remarks>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="folderName">The language folder, by prefix: English, French, German.</param>
  /// <returns>A task that completes once the language is active.</returns>
  // The step table ignores the keyword, so an action and an assertion cannot share their text:
  // declaring both as "the language is {string}" made every scenario in this file fail with
  // "Ambiguous step", found by running them.
  [When("the language is set to {string}", TimeoutSeconds = 40f)]
  public async Task SetLanguage(PickleContext ctx, string folderName) {
    // Folder names carry the native name too - "French (Français)" - so an exact match on "French"
    // finds nothing. Exact first, so "Russian" cannot take "Russian (Русский)" by accident.
    List<LoadedLanguage> all = LanguageDatabase.AllLoadedLanguages.ToList();
    LoadedLanguage? language = all.FirstOrDefault(l => l.folderName == folderName)
        ?? all.FirstOrDefault(l => l.folderName.StartsWith(folderName + " (", StringComparison.Ordinal));
    ctx.Require(
        language != null,
        $"no language matching '{folderName}'. installed: {string.Join(", ", all.Select(l => l.folderName))}");

    LanguageDatabase.SelectLanguage(language);
    await ctx.WaitUntil(() => LanguageDatabase.activeLanguage == language, 35f);
    ctx.Assert(
        LanguageDatabase.activeLanguage == language,
        $"the language did not become '{language!.folderName}' within 35s; it is " +
        $"'{LanguageDatabase.activeLanguage?.folderName ?? "none at all"}'");
  }

  /// <summary>Asserts which language is active.</summary>
  /// <param name="ctx">The scenario's context, for assertions, requirements, and waits.</param>
  /// <param name="folderName">The language folder, by prefix.</param>
  [Then("the language is {string}")]
  public void AssertLanguage(PickleContext ctx, string folderName) {
    string? active = LanguageDatabase.activeLanguage?.folderName;
    ctx.Assert(
        active != null && (active == folderName || active.StartsWith(folderName + " (", StringComparison.Ordinal)),
        $"expected the language to be '{folderName}'; it is '{active ?? "none at all"}'");
  }
}
