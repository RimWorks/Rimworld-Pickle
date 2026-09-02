using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RimWorks.Pickle.Fixtures;

/// <summary>Calls RimWorks.Quickstarts through reflection so Pickle keeps no reference to it.</summary>
public static class QuickstartBridge {
  private const string RegistryTypeName = "RimWorks.Quickstarts.QuickstartRegistry";
  private const string LookupTypeName = "RimWorks.Quickstarts.QuickstartLookup";
  private const string QuickstarterTypeName = "RimWorks.Quickstarts.Quickstarter";

  public static bool IsLoaded => FindType(RegistryTypeName) != null;

  /// <summary>Builds and launches the named quickstart, or throws saying why it could not.</summary>
  public static void Launch(string name) {
    Type registry = Require(RegistryTypeName);
    Type lookup = Require(LookupTypeName);
    Type quickstarter = Require(QuickstarterTypeName);

    object candidates = Get(registry, "AllTypes");

    // Resolve reports its own failure through an out param, and its message already lists
    // every known quickstart, so it beats anything Pickle could say here.
    object?[] args = [name, candidates, null];
    MethodInfo resolve = Method(lookup, "Resolve", 3);
    Type? resolved = (Type?)resolve.Invoke(null, args);

    if (resolved == null) {
      throw new InvalidOperationException($"pickle: {args[2] as string ?? $"no quickstart is called '{name}'"}");
    }

    object? instance = Method(registry, "Create", 1).Invoke(null, [resolved]);
    if (instance == null) {
      throw new InvalidOperationException($"pickle: quickstart '{resolved.Name}' could not be constructed");
    }

    Method(quickstarter, "Launch", 1).Invoke(null, [instance]);
  }

  /// <summary>Every quickstart name the loaded mods offer, for a failure message.</summary>
  public static IReadOnlyList<string> KnownNames() {
    Type? registry = FindType(RegistryTypeName);
    if (registry == null) {
      return [];
    }

    List<string> names = [];
    foreach (object item in (IEnumerable)Get(registry, "AllTypes")) {
      if (item is Type type) {
        names.Add(type.Name);
      }
    }

    names.Sort(StringComparer.OrdinalIgnoreCase);
    return names;
  }

  private static Type Require(string typeName) {
    return FindType(typeName)
        ?? throw new InvalidOperationException(
            $"pickle: a scenario asked for a quickstart but RimWorks.Quickstarts is not loaded");
  }

  // Type.GetType needs the assembly name and the mod may be packaged under either, so the
  // loaded assemblies are the only reliable place to look.
  private static Type? FindType(string typeName) {
    return AppDomain.CurrentDomain.GetAssemblies()
        .Select(a => SafeGetType(a, typeName))
        .FirstOrDefault(t => t != null);
  }

  private static Type? SafeGetType(Assembly assembly, string typeName) {
    try {
      return assembly.GetType(typeName, throwOnError: false);
    } catch (Exception) {
      // A mod assembly that cannot resolve its own references throws here; skip it.
      return null;
    }
  }

  private static object Get(Type type, string propertyName) {
    PropertyInfo property = type.GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static)
        ?? throw new InvalidOperationException($"pickle: {type.FullName}.{propertyName} is missing");

    return property.GetValue(null)
        ?? throw new InvalidOperationException($"pickle: {type.FullName}.{propertyName} returned null");
  }

  private static MethodInfo Method(Type type, string methodName, int parameterCount) {
    return type.GetMethods(BindingFlags.Public | BindingFlags.Static)
        .FirstOrDefault(m => m.Name == methodName && m.GetParameters().Length == parameterCount)
        ?? throw new InvalidOperationException(
            $"pickle: {type.FullName}.{methodName} with {parameterCount} argument(s) is missing");
  }
}
