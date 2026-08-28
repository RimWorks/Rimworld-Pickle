using System;
using System.Collections.Generic;

namespace Pickle.Core.Model;

public class StepPlan {
  public StepPlan(string keyword, string text, IReadOnlyList<IReadOnlyList<string>> table, string? docString, int line) {
    Keyword = keyword;
    Text = text;
    Table = table;
    DocString = docString;
    Line = line;
  }

  public string Keyword { get; }

  public string Text { get; }

  public IReadOnlyList<IReadOnlyList<string>> Table { get; }

  public string? DocString { get; }

  public int Line { get; }
}
