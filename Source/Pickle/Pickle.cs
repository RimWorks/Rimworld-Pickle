using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Steps;

namespace RimWorks.Pickle;

public static class Pickle {
  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Given(string pattern, Action<PickleContext> body) {
    FluentRegistry.Register(pattern, StepKind.Given, body, Assembly.GetCallingAssembly());
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Given(string pattern, Func<PickleContext, Task> body) {
    FluentRegistry.Register(pattern, StepKind.Given, body, Assembly.GetCallingAssembly());
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void When(string pattern, Action<PickleContext> body) {
    FluentRegistry.Register(pattern, StepKind.When, body, Assembly.GetCallingAssembly());
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void When(string pattern, Func<PickleContext, Task> body) {
    FluentRegistry.Register(pattern, StepKind.When, body, Assembly.GetCallingAssembly());
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Then(string pattern, Action<PickleContext> body) {
    FluentRegistry.Register(pattern, StepKind.Then, body, Assembly.GetCallingAssembly());
  }

  [MethodImpl(MethodImplOptions.NoInlining)]
  public static void Then(string pattern, Func<PickleContext, Task> body) {
    FluentRegistry.Register(pattern, StepKind.Then, body, Assembly.GetCallingAssembly());
  }
}
