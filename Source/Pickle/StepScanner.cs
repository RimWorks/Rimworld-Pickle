using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using RimWorks.Pickle.Core.Steps;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle;

/// <summary>Finds step definitions across loaded assemblies, whether declared as attributed
/// methods or registered fluently through a <see cref="PickleEntryAttribute"/> class.</summary>
public static class StepScanner {
  /// <summary>Builds a step table from every <see cref="PickleStepsAttribute"/> class in the given
  /// assemblies, running entry points first so fluent registrations land too.</summary>
  /// <param name="assemblies">The assemblies to scan.</param>
  /// <returns>A step table ready to resolve step text against.</returns>
  public static StepTable PopulateStepTable(IEnumerable<Assembly> assemblies) {
    StepTable table = new StepTable();

    InvokeEntryPoints(assemblies, table);

    foreach (Assembly assembly in assemblies) {
      Type[] types = GetLoadableTypes(assembly);
      foreach (Type type in types) {
        if (Has<PickleStepsAttribute>(type)) {
          ScanStepsClass(type, table);
        }
      }
    }

    return table;
  }

  /// <summary>Finds every class carrying <see cref="PickleStepsAttribute"/>, for callers that need
  /// the types themselves rather than the compiled step table.</summary>
  /// <param name="assemblies">The assemblies to scan.</param>
  /// <returns>The matching step classes.</returns>
  public static List<Type> GetPickleStepsTypes(IEnumerable<Assembly> assemblies) {
    List<Type> stepsTypes = new();

    foreach (Assembly assembly in assemblies) {
      Type[] types = GetLoadableTypes(assembly);
      foreach (Type type in types) {
        if (Has<PickleStepsAttribute>(type)) {
          stepsTypes.Add(type);
        }
      }
    }

    return stepsTypes;
  }

  /// <summary>Calls the static <c>Init()</c> on every <see cref="PickleEntryAttribute"/> class, then
  /// drains whatever fluent steps that registered into the table.</summary>
  /// <param name="assemblies">The assemblies to scan for entry points.</param>
  /// <param name="table">The step table the drained registrations are added to.</param>
  public static void InvokeEntryPoints(IEnumerable<Assembly> assemblies, StepTable table) {
    foreach (Assembly assembly in assemblies) {
      Type[] types = GetLoadableTypes(assembly);
      foreach (Type type in types) {
        if (Has<PickleEntryAttribute>(type)) {
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

  // Reading an attribute off a type salvaged from a ReflectionTypeLoadException throws
  // again when the attribute's own assembly is the missing one. Quiet, because a broken
  // mod would otherwise log a line for every type it ships.
  private static bool Has<T>(Type type)
      where T : Attribute {
    try {
      return type.GetCustomAttribute<T>() != null;
    } catch (Exception) {
      return false;
    }
  }

  // One unloadable type sinks GetTypes() for a whole assembly, so a stale steps dll
  // could take out every other mod's steps. Log it and keep what did load.
  private static Type[] GetLoadableTypes(Assembly assembly) {
    try {
      return assembly.GetTypes();
    } catch (ReflectionTypeLoadException ex) {
      string reasons = string.Join("; ", ex.LoaderExceptions.Select(e => e?.Message ?? "unknown"));
      Log.WarnTo(PickleLog.Channel,
          "{Assembly} has unloadable types, skipping them: {Reasons}",
          [assembly.GetName().Name, reasons]);
      return ex.Types.Where(t => t != null).ToArray()!;
    } catch (Exception ex) {
      // A dynamic or half-loaded assembly throws something else entirely, and losing one
      // assembly's steps beats losing every mod's.
      Log.WarnTo(PickleLog.Channel,
          "{Assembly} could not be scanned for steps: {Reason}",
          [assembly.GetName().Name, ex.Message]);
      return [];
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
