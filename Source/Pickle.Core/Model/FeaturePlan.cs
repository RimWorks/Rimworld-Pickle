using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Model;

/// <summary>One <c>.feature</c> file after <see cref="GherkinAdapter"/> has flattened rules, backgrounds
/// and outlines into a plain list of runnable scenarios.</summary>
public class FeaturePlan {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="name">The feature's name.</param>
  /// <param name="tags">The tags declared directly on the feature.</param>
  /// <param name="scenarios">Every scenario in the feature, outlines already expanded into one entry per row.</param>
  /// <param name="sourcePath">The path the feature was read from, or <c>null</c> when it has none.</param>
  public FeaturePlan(string name, TagSet tags, IReadOnlyList<ScenarioPlan> scenarios, string? sourcePath) {
    Name = name;
    Tags = tags;
    Scenarios = scenarios;
    SourcePath = sourcePath;
  }

  /// <summary>The feature's name.</summary>
  public string Name { get; }

  /// <summary>The tags declared directly on the feature.</summary>
  public TagSet Tags { get; }

  /// <summary>Every scenario in the feature, outlines already expanded into one entry per row.</summary>
  public IReadOnlyList<ScenarioPlan> Scenarios { get; }

  /// <summary>The path the feature was read from, or <c>null</c> when it has none.</summary>
  public string? SourcePath { get; }
}
