using System;
using System.Collections.Generic;
using System.Reflection;
using Pickle.Core.Steps;

namespace Pickle;

internal static class FluentRegistry {
  private static readonly List<StepDefinition> PendingDefinitions = [];

  public static void Register(string pattern, StepKind kind, object body, Assembly caller) {
    string callerName = caller.GetName().Name ?? "Unknown";
    string source = $"Pickle.{kind}(...) ({callerName})";
    PendingDefinitions.Add(new StepDefinition(pattern, kind, source, Array.Empty<Type>(), body));
  }

  public static IReadOnlyList<StepDefinition> DrainPending() {
    List<StepDefinition> result = [.. PendingDefinitions];
    PendingDefinitions.Clear();
    return result;
  }
}
