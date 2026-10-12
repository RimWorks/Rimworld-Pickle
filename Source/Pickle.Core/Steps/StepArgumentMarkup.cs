using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace RimWorks.Pickle.Core.Steps;

/// <summary>Markup that sets a step's captured values apart from the prose around them, for the in-game runner and the
/// dashboard alike.</summary>
public static class StepArgumentMarkup {
  private static readonly Regex LiteralPattern =
      new Regex("\"[^\"]*\"|'[^']*'|\\b\\d+(?:\\.\\d+)?\\b", RegexOptions.None, TimeSpan.FromSeconds(2));

  /// <summary>Marks each argument in a step's text.</summary>
  /// <param name="text">The step text, without its Gherkin keyword.</param>
  /// <param name="spans">Where the matched arguments sit, or <c>null</c> for a step that never matched a definition.</param>
  /// <param name="open">Markup placed before each argument.</param>
  /// <param name="close">Markup placed after each argument.</param>
  /// <returns>The step text with every argument wrapped. A step with no spans falls back to quoted strings and
  /// numbers, which is what a reader sees before a run has resolved the step against the step table.</returns>
  public static string Apply(string text, IReadOnlyList<StepArgument>? spans, string open, string close) {
    if (string.IsNullOrEmpty(text)) {
      return text;
    }

    if (spans == null || spans.Count == 0) {
      return LiteralPattern.Replace(text, match => open + match.Value + close);
    }

    StringBuilder marked = new StringBuilder(text.Length + (spans.Count * (open.Length + close.Length)));
    int at = 0;
    foreach (StepArgument span in spans.OrderBy(span => span.Start)) {
      if (span.Start < at || span.Length <= 0 || span.Start + span.Length > text.Length) {
        continue;
      }

      marked.Append(text, at, span.Start - at).Append(open).Append(text, span.Start, span.Length).Append(close);
      at = span.Start + span.Length;
    }

    return marked.Append(text, at, text.Length - at).ToString();
  }
}
