using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorks.Pickle.Core.Steps;
using Verse;

namespace RimWorks.Pickle;

public static class StepScanner {
  public static StepTable PopulateStepTable(IEnumerable<Assembly> assemblies) {
    StepTable table = new StepTable();

    InvokeEntryPoints(assemblies, table);

    foreach (Assembly assembly in assemblies) {
      Type[] types = GetLoadableTypes(assembly);
      foreach (Type type in types) {
        if (type.GetCustomAttribute<PickleStepsAttribute>() != null) {
          ScanStepsClass(type, table);
        }
      }
    }

    return table;
  }

  public static List<Type> GetPickleStepsTypes(IEnumerable<Assembly> assemblies) {
    List<Type> stepsTypes = new();

    foreach (Assembly assembly in assemblies) {
      Type[] types = GetLoadableTypes(assembly);
      foreach (Type type in types) {
        if (type.GetCustomAttribute<PickleStepsAttribute>() != null) {
          stepsTypes.Add(type);
        }
      }
    }

    return stepsTypes;
  }

  public static void InvokeEntryPoints(IEnumerable<Assembly> assemblies, StepTable table) {
    foreach (Assembly assembly in assemblies) {
      Type[] types = GetLoadableTypes(assembly);
      foreach (Type type in types) {
        if (type.GetCustomAttribute<PickleEntryAttribute>() != null) {
          MethodInfo? initMethod = type.GetMethod("Init", BindingFlags.Static | BindingFlags.Public);
          if (initMethod != null && initMethod.ReturnType == typeof(void)) {
            initMethod.Invoke(null, null);
          }
        }
      }
    }

    foreach (StepDefinition def in FluentRegistry.DrainPending()) {
      table.Add(def);
    }
  }

  // One unloadable type sinks GetTypes() for a whole assembly, so a stale steps dll
  // could take out every other mod's steps. Log it and keep what did load.
  private static Type[] GetLoadableTypes(Assembly assembly) {
    try {
      return assembly.GetTypes();
    } catch (ReflectionTypeLoadException ex) {
      string reasons = string.Join("; ", ex.LoaderExceptions.Select(e => e?.Message ?? "unknown"));
      Log.Warning($"pickle: {assembly.GetName().Name} has unloadable types, skipping them: {reasons}");
      return ex.Types.Where(t => t != null).ToArray()!;
    }
  }

  private static void ScanStepsClass(Type stepsClass, StepTable table) {
    MethodInfo[] methods = stepsClass.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.Static);

    foreach (MethodInfo method in methods) {
      GivenAttribute? givenAttr = method.GetCustomAttribute<GivenAttribute>();
      if (givenAttr != null) {
        StepDefinition def = CreateStepDefinition(
            stepsClass, method, givenAttr.Pattern, StepKind.Given, givenAttr.TimeoutSeconds);
        table.Add(def);
        continue;
      }

      WhenAttribute? whenAttr = method.GetCustomAttribute<WhenAttribute>();
      if (whenAttr != null) {
        StepDefinition def = CreateStepDefinition(
            stepsClass, method, whenAttr.Pattern, StepKind.When, whenAttr.TimeoutSeconds);
        table.Add(def);
        continue;
      }

      ThenAttribute? thenAttr = method.GetCustomAttribute<ThenAttribute>();
      if (thenAttr != null) {
        StepDefinition def = CreateStepDefinition(
            stepsClass, method, thenAttr.Pattern, StepKind.Then, thenAttr.TimeoutSeconds);
        table.Add(def);
      }
    }
  }

  private static StepDefinition CreateStepDefinition(
      Type stepsClass,
      MethodInfo method,
      string pattern,
      StepKind kind,
      float timeoutSeconds) {
    ParameterInfo[] parameters = method.GetParameters();
    List<Type> parameterTypes = new();

    if (parameters.Length > 0 && parameters[0].ParameterType == typeof(PickleContext)) {
      for (int i = 1; i < parameters.Length; i++) {
        parameterTypes.Add(parameters[i].ParameterType);
      }
    }

    string assemblyName = stepsClass.Assembly.GetName().Name ?? "Unknown";
    string source = $"{stepsClass.Name}.{method.Name} ({assemblyName})";

    return new StepDefinition(
        pattern,
        kind,
        source,
        parameterTypes,
        method,
        timeoutSeconds > 0f ? timeoutSeconds : null);
  }
}
