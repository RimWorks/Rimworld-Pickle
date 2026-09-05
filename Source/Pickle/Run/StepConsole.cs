using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorks.Pickle.Autorun;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;
using RimWorks.Pickle.Web;

namespace RimWorks.Pickle.Run;

/// <summary>
/// Runs one step at a time against the running game, for the dashboard console. The
/// context survives between steps, unlike a scenario's, so state set by one step is
/// still there for the next.
/// </summary>
public static class StepConsole {
  private static RunSession? session;
  private static StepTable? table;
  private static PickleContext context = new PickleContext();

  public static IReadOnlyList<StepDefinition> Definitions => table?.Definitions ?? [];

  /// <summary>Resolves and runs one step. Call on the main thread.</summary>
  public static async Task<(StepResult Result, List<(string Source, string Content)> StateDumps)> Run(string text) {
    // No paramName: this message goes to a browser, and the framework would append
    // "Parameter name: text" to whatever a person reads.
    if (string.IsNullOrWhiteSpace(text)) {
      throw new ArgumentException("Type a step to run.");
    }

    RunSession live = Ensure();
    StepResult result = await live.RunOneStep(context, text);
    return (result, StateDumpCollector.Collect(live.ScenarioInstances()));
  }

  /// <summary>Loads the step table without running anything, so the catalogue can be listed.</summary>
  public static void EnsureLoaded() {
    _ = Ensure();
  }

  /// <summary>Drops the shared context. The step table and its instances survive.</summary>
  public static void Reset() {
    context = new PickleContext();
  }

  internal static void RefuseWhenBusy() {
    if (AutorunState.IsAutorunning) {
      throw new InvalidOperationException("An unattended run owns the game. The console is off during autorun.");
    }

    if (RunnerWindow.Instance.IsRunning || FixtureCommands.IsBusy) {
      throw new InvalidOperationException("Wait for the current run or fixture operation to finish.");
    }
  }

  private static RunSession Ensure() {
    if (session != null) {
      return session;
    }

    (StepTable built, List<Type> stepsTypes, List<DiscoveredSuite> suites) = SuiteRunner.BuildStepEnvironment();
    table = built;
    session = new RunSession(built, PickleDriver.Instance, suites, stepsTypes);
    return session;
  }
}
