using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Model;

/// <summary>One Gherkin step, expanded from an outline row and stripped of anything scenario outline specific.</summary>
public class StepPlan {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="keyword">The Given, When, Then or And text as written in the feature file.</param>
  /// <param name="text">The step text, with any outline placeholders already substituted.</param>
  /// <param name="table">The step's data table as rows of cell values, empty when the step has none.</param>
  /// <param name="docString">The step's doc string, or <c>null</c> when the step has none.</param>
  /// <param name="line">The 1-based line the step starts on in its source feature file.</param>
  public StepPlan(string keyword, string text, IReadOnlyList<IReadOnlyList<string>> table, string? docString, int line) {
    Keyword = keyword;
    Text = text;
    Table = table;
    DocString = docString;
    Line = line;
  }

  /// <summary>The Given, When, Then or And text as written in the feature file.</summary>
  public string Keyword { get; }

  /// <summary>The step text, with any outline placeholders already substituted.</summary>
  public string Text { get; }

  /// <summary>The step's data table as rows of cell values, empty when the step has none.</summary>
  public IReadOnlyList<IReadOnlyList<string>> Table { get; }

  /// <summary>The step's doc string, or <c>null</c> when the step has none.</summary>
  public string? DocString { get; }

  /// <summary>The 1-based line the step starts on in its source feature file.</summary>
  public int Line { get; }
}
