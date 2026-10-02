using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Ui;

/// <summary>
/// Remembers which button to click when a window of a given type opens. Split from the
/// Verse-facing side so the matching stays unit testable, the same way WindowSuppressionRule is.
/// </summary>
public sealed class DialogAnswers {
  private readonly Dictionary<string, string> byWindowType = new(StringComparer.Ordinal);

  /// <summary>Whether any answer is registered, so a caller can skip its per-frame work.</summary>
  public bool Any => byWindowType.Count > 0;

  /// <summary>Records the button to click when a window of this type opens, replacing any earlier answer for it.</summary>
  /// <param name="windowTypeName">The window's simple type name, as <c>GetType().Name</c> reports it.</param>
  /// <param name="buttonLabel">The label on the button to click.</param>
  /// <exception cref="ArgumentException">Either argument is empty or blank.</exception>
  public void Add(string? windowTypeName, string? buttonLabel) {
    string type = windowTypeName?.Trim() ?? string.Empty;
    string label = buttonLabel?.Trim() ?? string.Empty;

    if (type.Length == 0) {
      throw new ArgumentException("a dialog answer needs a window type name", nameof(windowTypeName));
    }

    if (label.Length == 0) {
      throw new ArgumentException("a dialog answer needs a button label", nameof(buttonLabel));
    }

    byWindowType[type] = label;
  }

  /// <summary>Looks an answer up by exact, case sensitive type name. A base class or a namespace never matches.</summary>
  /// <param name="windowTypeName">The window's simple type name.</param>
  /// <param name="buttonLabel">The label to click, when one is registered.</param>
  /// <returns><c>true</c> when an answer is registered for the type.</returns>
  public bool TryGet(string? windowTypeName, out string buttonLabel) {
    if (windowTypeName != null && byWindowType.TryGetValue(windowTypeName, out string? found)) {
      buttonLabel = found;
      return true;
    }

    buttonLabel = string.Empty;
    return false;
  }

  /// <summary>Forgets every answer, so one scenario's rules never reach the next.</summary>
  public void Clear() {
    byWindowType.Clear();
  }
}
