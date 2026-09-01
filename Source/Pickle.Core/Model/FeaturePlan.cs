using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Model;

public class FeaturePlan {
  public FeaturePlan(string name, TagSet tags, IReadOnlyList<ScenarioPlan> scenarios, string? sourcePath) {
    Name = name;
    Tags = tags;
    Scenarios = scenarios;
    SourcePath = sourcePath;
  }

  public string Name { get; }

  public TagSet Tags { get; }

  public IReadOnlyList<ScenarioPlan> Scenarios { get; }

  public string? SourcePath { get; }
}
