using System;
using System.Collections.Generic;

namespace Pickle.Core.Model;

public class ScenarioPlan {
  public ScenarioPlan(string name, TagSet tags, IReadOnlyList<StepPlan> steps, int line) {
    Name = name;
    Tags = tags;
    Steps = steps;
    Line = line;
  }

  public string Name { get; }

  public TagSet Tags { get; }

  public IReadOnlyList<StepPlan> Steps { get; }

  public int Line { get; }
}
