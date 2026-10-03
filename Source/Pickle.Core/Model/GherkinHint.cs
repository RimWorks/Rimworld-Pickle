using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Model;

/// <summary>
/// Turns a Gherkin parser error into a line a person can act on. The parser names the token types
/// it wanted, which says what is missing without reading its wording.
/// </summary>
public static class GherkinHint {
  /// <summary>The hint for a dialect the parser does not ship. The parser's own line names which one.</summary>
  public const string UnknownLanguage = "gherkin has no dialect by the name this `# language:` header asks for";

  private static readonly (string Token, string Hint)[] ByExpected = [
    ("#FeatureLine", "a feature starts with a `Feature:` line, before any scenario"),
    ("#StepLine", "a scenario needs at least one `Given`, `When` or `Then` step"),
    ("#ExamplesLine", "a `Scenario Outline` needs an `Examples:` table under it"),
    ("#ScenarioLine", "add a `Scenario:` line, indented under the feature"),
    ("#DocStringSeparator", "close the doc string with the same fence that opened it"),
    ("#TableRow", "a table row looks like `| one | two |`, with a pipe at each end"),
    ("#BackgroundLine", "a `Background:` goes above the scenarios, and only once"),
    ("#RuleLine", "a `Rule:` groups scenarios, and goes under the feature"),
    ("#TagLine", "a tag line holds only tags, like `@slow @wip`"),
    ("#Language", "a `# language:` header has to be the first line in the file"),
  ];

  /// <summary>The hint for an unexpected token, chosen from the token types the parser wanted.</summary>
  /// <param name="expectedTokenTypes">The parser's expected token names, each already carrying its <c>#</c>.</param>
  /// <param name="atEndOfFile">Whether the parser ran out of input rather than reading a wrong token.</param>
  /// <returns>A hint, or <c>null</c> when nothing in the expected set is worth explaining.</returns>
  public static string? For(IReadOnlyList<string>? expectedTokenTypes, bool atEndOfFile) {
    if (expectedTokenTypes == null || expectedTokenTypes.Count == 0) {
      return null;
    }

    foreach ((string token, string hint) in ByExpected) {
      if (Holds(expectedTokenTypes, token)) {
        return atEndOfFile ? hint + ", and the text ends before it arrives" : hint;
      }
    }

    if (Holds(expectedTokenTypes, "#EOF")) {
      return "nothing may follow the end of the feature";
    }

    return null;
  }

  private static bool Holds(IReadOnlyList<string> expected, string token) {
    for (int i = 0; i < expected.Count; i++) {
      if (string.Equals(expected[i], token, System.StringComparison.OrdinalIgnoreCase)) {
        return true;
      }
    }

    return false;
  }
}
