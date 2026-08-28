using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using LudeonTK;
using RimWorld;
using Verse;

namespace Pickle.Vanilla;

/// <summary>
/// Dev mode toggles and debug actions. A debug action is a static method tagged with
/// [DebugAction], so Pickle calls the method instead of driving the debug menu.
/// </summary>
[PickleSteps]
public static class DevModeSteps {
  private static List<(string Name, string Category, MethodInfo Method)>? cachedActions;

  [Given("dev mode is enabled")]
  public static void EnableDevMode(PickleContext ctx) {
    Prefs.DevMode = true;
  }

  [Given("god mode is enabled")]
  public static void EnableGodMode(PickleContext ctx) {
    Prefs.DevMode = true;
    DebugSettings.godMode = true;
  }

  [Given("god mode is disabled")]
  public static void DisableGodMode(PickleContext ctx) {
    DebugSettings.godMode = false;
  }

  [When("I trigger debug action {string}")]
  public static void TriggerAction(PickleContext ctx, string name) {
    Invoke(ctx, name, null);
  }

  [When("I trigger debug action {string} in category {string}")]
  public static void TriggerActionInCategory(PickleContext ctx, string name, string category) {
    Invoke(ctx, name, category);
  }

  private static void Invoke(PickleContext ctx, string name, string? category) {
    string where = category == null ? $"'{name}'" : $"'{name}' in category '{category}'";
    (string Name, string Category, MethodInfo Method) action = AllActions().FirstOrDefault(a =>
        a.Name.Equals(name, StringComparison.OrdinalIgnoreCase) &&
        (category == null || a.Category.Equals(category, StringComparison.OrdinalIgnoreCase)));

    ctx.Require(action.Method != null, $"no debug action {where}. {SuggestNames(name)}");

    MethodInfo method = action.Method!;
    ParameterInfo[] parameters = method.GetParameters();
    ctx.Require(
        parameters.Length == 0,
        $"debug action {where} needs a target, so Pickle cannot call it. " +
        $"it expects: {string.Join(", ", parameters.Select(p => p.ParameterType.Name))}");

    Prefs.DevMode = true;
    method.Invoke(null, null);
  }

  private static string SuggestNames(string name) {
    string prefix = name.Length < 3 ? name : name[..3];
    string[] near = [.. AllActions()
        .Where(a => a.Name.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        .Select(a => $"{a.Category}/{a.Name}")
        .Take(5)];

    return near.Length == 0 ? "no similar names found" : $"did you mean: {string.Join(", ", near)}";
  }

  private static List<(string Name, string Category, MethodInfo Method)> AllActions() {
    return cachedActions ??= [.. Scan()];
  }

  private static IEnumerable<(string Name, string Category, MethodInfo Method)> Scan() {
    foreach (Type type in GenTypes.AllTypes) {
      MethodInfo[] methods;
      try {
        methods = type.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
      } catch (Exception) {
        // a type whose dependencies failed to load throws here
        continue;
      }

      foreach (MethodInfo method in methods) {
        if (method.TryGetAttribute(out DebugActionAttribute attribute)) {
          yield return (attribute.name ?? method.Name, attribute.category ?? string.Empty, method);
        }
      }
    }
  }
}
