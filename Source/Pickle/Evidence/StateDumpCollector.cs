using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace RimWorks.Pickle.Evidence;

public static class StateDumpCollector {
  public static List<(string Source, string Content)> Collect(
      IEnumerable<KeyValuePair<Type, object>> stepInstanceCache) {
    List<(string Source, string Content)> dumps = [];

    foreach (KeyValuePair<Type, object> kvp in stepInstanceCache) {
      Type stepType = kvp.Key;
      object instance = kvp.Value;

      MethodInfo[] methods = stepType.GetMethods(
          BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

      foreach (MethodInfo method in methods) {
        PickleStateDumpAttribute? attr = method.GetCustomAttribute<PickleStateDumpAttribute>();
        if (attr == null) {
          continue;
        }

        string source = $"{stepType.Name}.{method.Name}";
        string content;

        try {
          object? result = method.Invoke(
              method.IsStatic ? null : instance,
              []);

          content = result?.ToString() ?? string.Empty;
        } catch (Exception ex) {
          content = $"dump threw: {ex.InnerException?.Message ?? ex.Message}";
        }

        dumps.Add((source, content));
      }
    }

    return dumps;
  }
}
