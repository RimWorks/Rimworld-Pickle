using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Steps;

namespace RimWorks.Pickle;

/// <summary>Registers steps as delegates instead of attributed methods. An alternative to
/// <see cref="GivenAttribute"/>, <see cref="WhenAttribute"/> and <see cref="ThenAttribute"/>,
/// meant to be called from a <see cref="PickleEntryAttribute"/> class's <c>Init()</c>.</summary>
public static class Pickle {
  /// <summary>Registers a synchronous <c>Given</c> step.</summary>
  /// <param name="pattern">The cucumber expression to match step text against.</param>
  /// <param name="body">The step's body.</param>
  // NoInlining keeps this frame on the stack, which is what GetCallingAssembly needs to
  // report the mod that called Given rather than Pickle itself.
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Given(string pattern, Action<PickleContext> body) {
    FluentRegistry.Register(pattern, StepKind.Given, body, Assembly.GetCallingAssembly());
  }

  /// <summary>Registers an asynchronous <c>Given</c> step.</summary>
  /// <param name="pattern">The cucumber expression to match step text against.</param>
  /// <param name="body">The step's body.</param>
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Given(string pattern, Func<PickleContext, Task> body) {
    FluentRegistry.Register(pattern, StepKind.Given, body, Assembly.GetCallingAssembly());
  }

  /// <summary>Registers a synchronous <c>When</c> step.</summary>
  /// <param name="pattern">The cucumber expression to match step text against.</param>
  /// <param name="body">The step's body.</param>
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void When(string pattern, Action<PickleContext> body) {
    FluentRegistry.Register(pattern, StepKind.When, body, Assembly.GetCallingAssembly());
  }

  /// <summary>Registers an asynchronous <c>When</c> step.</summary>
  /// <param name="pattern">The cucumber expression to match step text against.</param>
  /// <param name="body">The step's body.</param>
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void When(string pattern, Func<PickleContext, Task> body) {
    FluentRegistry.Register(pattern, StepKind.When, body, Assembly.GetCallingAssembly());
  }

  /// <summary>Registers a synchronous <c>Then</c> step.</summary>
  /// <param name="pattern">The cucumber expression to match step text against.</param>
  /// <param name="body">The step's body.</param>
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Then(string pattern, Action<PickleContext> body) {
    FluentRegistry.Register(pattern, StepKind.Then, body, Assembly.GetCallingAssembly());
  }

  /// <summary>Registers an asynchronous <c>Then</c> step.</summary>
  /// <param name="pattern">The cucumber expression to match step text against.</param>
  /// <param name="body">The step's body.</param>
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Then(string pattern, Func<PickleContext, Task> body) {
    FluentRegistry.Register(pattern, StepKind.Then, body, Assembly.GetCallingAssembly());
  }
}
