using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gherkin.Ast;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using RimWorks.Pickle.Run;
using RimWorks.Pickle.Web;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

public static class SuiteRunner {
  public static async Task<List<ScenarioResult>> Run(
      string? filter = null,
      int seed = RunSession.DefaultSeed,
      Action<ScenarioResult>? onScenarioCompleted = null,
      int retries = 0) {
    List<ScenarioResult> allResults = new();

    try {
      (StepTable stepTable, List<Type> stepsTypes, List<DiscoveredSuite> discoveredSuites) = BuildStepEnvironment();
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures = ScenarioFilter.FilterFeatures(FeatureParser.ParseAll(discoveredSuites), filter);

      RunSession session = new RunSession(stepTable, PickleDriver.Instance, discoveredSuites, stepsTypes, seed, retries);
      PickleHttpServer.ActiveSession = session;

      Dictionary<(string SourcePath, int ScenarioIndex), ScenarioResult> published = new();
      void PublishSnapshot() =>
          PickleHttpServer.Publish(RunnerSnapshot.Build(parsedFeatures, published, session, true));
      session.OnProgress = PublishSnapshot;
      PublishSnapshot();

      int totalFailed = 0;
      int featureStartIndex = 0;
      List<(string Name, string? Message)> failedScenarios = new();

      foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
        string sourcePath = plan.SourcePath ?? string.Empty;

        // wip/tag filtering means completion order does not track plan order,
        // so results are placed by scenario name rather than by a counter.
        List<ScenarioResult> featureResults = await session.RunFeature(plan, suite.ModName, IncludeWipState.Enabled, result => {
          for (int i = 0; i < plan.Scenarios.Count; i++) {
            if (plan.Scenarios[i].Name == result.ScenarioName) {
              published[(sourcePath, featureStartIndex + i)] = result;
              break;
            }
          }

          PublishSnapshot();
          onScenarioCompleted?.Invoke(result);
        });
        allResults.AddRange(featureResults);
        featureStartIndex += plan.Scenarios.Count;

        foreach (ScenarioResult result in featureResults) {
          if (result.Outcome == ScenarioOutcome.Failed) {
            totalFailed++;
            failedScenarios.Add(($"{result.FeatureName}: {result.ScenarioName}", result.FailureMessage));
          }
        }
      }

      if (totalFailed == 0) {
        Log.Info("pickle: suite passed");
      } else {
        Log.Info("pickle: suite failed");
        foreach ((string name, string? message) in failedScenarios) {
          Log.Error("pickle: failed: {Name}", [name]);
          if (!string.IsNullOrEmpty(message)) {
            Log.Error("  {Message}", [message]);
          }
        }
      }
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, "pickle: suite runner error");
      throw;
    }

    return allResults;
  }

  /// <summary>
  /// Scans every loaded suite and builds the step table a session runs against. The
  /// console builds its own rather than sharing a run's, because a RunSession registers
  /// the engine steps into whatever table it is handed and two sessions on one table
  /// would make every engine step ambiguous.
  /// </summary>
  internal static (StepTable Table, List<Type> StepsTypes, List<DiscoveredSuite> Suites) BuildStepEnvironment() {
    List<DiscoveredSuite> suites = SuiteScanner.DiscoverSuites();
    List<Assembly> assemblies = BuildAssemblyList(suites);
    return (StepScanner.PopulateStepTable(assemblies), StepScanner.GetPickleStepsTypes(assemblies), suites);
  }

  private static List<Assembly> BuildAssemblyList(List<DiscoveredSuite> discoveredSuites) {
    List<Assembly> assemblies = new()
    {
            typeof(SuiteRunner).Assembly,
    };

    Type? vanillaType = Type.GetType("RimWorks.Pickle.Vanilla.UiSteps, RimWorks.Pickle.Vanilla");
    if (vanillaType != null) {
      assemblies.Add(vanillaType.Assembly);
    } else {
      foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
        if (a.GetName().Name == "RimWorks.Pickle.Vanilla") {
          assemblies.Add(a);
          break;
        }
      }
    }

    foreach (DiscoveredSuite suite in discoveredSuites) {
      foreach (string stepsDll in suite.StepsDlls) {
        try {
          Assembly loaded = Assembly.LoadFrom(stepsDll);
          assemblies.Add(loaded);
        } catch (Exception ex) {
          Log.Warn(ex, $"pickle: failed to load steps dll {Path.GetFileName(stepsDll)}");
        }
      }
    }

    return assemblies;
  }
}
