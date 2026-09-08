using System;
using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Model;

/// <summary>One scenario ready to run: its name, tags, and steps with any background and outline
/// expansion already folded in.</summary>
public class ScenarioPlan {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="name">The scenario's name, with any outline placeholders already substituted.</param>
  /// <param name="tags">The scenario's own tags merged with its feature's and rule's.</param>
  /// <param name="steps">The scenario's steps, with the feature or rule background steps first.</param>
  /// <param name="line">The 1-based line the scenario starts on in its source feature file.</param>
  public ScenarioPlan(string name, TagSet tags, IReadOnlyList<StepPlan> steps, int line) {
    Name = name;
    Tags = tags;
    Steps = steps;
    Line = line;
  }

  /// <summary>The scenario's name, with any outline placeholders already substituted.</summary>
  public string Name { get; }

  /// <summary>The scenario's own tags merged with its feature's and rule's.</summary>
  public TagSet Tags { get; }

  /// <summary>The scenario's steps, with the feature or rule background steps first.</summary>
  public IReadOnlyList<StepPlan> Steps { get; }

  /// <summary>The 1-based line the scenario starts on in its source feature file.</summary>
  public int Line { get; }
}
