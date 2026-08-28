using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using Gherkin;
using Gherkin.Ast;
using Pickle.Core;
using Pickle.Core.Discovery;
using Pickle.Core.Model;
using Pickle.Core.Run;
using Pickle.Core.Steps;
using Pickle.Run;
using Pickle.Web;
using Verse;

namespace Pickle.Runtime;

public static class SuiteRunner {
  public static async Task<List<ScenarioResult>> Run(
      string? filter = null,
      bool includeWip = false,
      int seed = RunSession.DefaultSeed,
      Action<ScenarioResult>? onScenarioCompleted = null) {
    List<ScenarioResult> allResults = new();

    try {
      List<DiscoveredSuite> discoveredSuites = SuiteScanner.DiscoverSuites();
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures = FilterFeatures(ParseFeatures(discoveredSuites), filter);

      List<Assembly> assemblies = BuildAssemblyList(discoveredSuites);
      StepTable stepTable = StepScanner.PopulateStepTable(assemblies);
      List<Type> stepsTypes = StepScanner.GetPickleStepsTypes(assemblies);

      RunSession session = new RunSession(stepTable, PickleDriver.Instance, discoveredSuites, stepsTypes, seed);
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
        List<ScenarioResult> featureResults = await session.RunFeature(plan, suite.ModName, includeWip, result => {
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
        Log.Message("pickle: suite passed");
      } else {
        Log.Message("pickle: suite failed");
        foreach ((string name, string? message) in failedScenarios) {
          Log.Error($"pickle: failed: {name}");
          if (!string.IsNullOrEmpty(message)) {
            Log.Error($"  {message}");
          }
        }
      }
    } catch (Exception ex) {
      Log.Error($"pickle: suite runner error: {ex.Message}\n{ex.StackTrace}");
      throw;
    }

    return allResults;
  }

  private static List<(DiscoveredSuite Suite, FeaturePlan Plan)> FilterFeatures(
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsedFeatures, string? filter) {
    if (string.IsNullOrEmpty(filter)) {
      return parsedFeatures;
    }

    if (filter!.StartsWith('@')) {
      List<(DiscoveredSuite Suite, FeaturePlan Plan)> tagFiltered = new();
      foreach ((DiscoveredSuite suite, FeaturePlan plan) in parsedFeatures) {
        List<ScenarioPlan> scenarios = [.. plan.Scenarios.Where(s => s.Tags.Contains(filter))];
        if (scenarios.Count > 0) {
          tagFiltered.Add((suite, new FeaturePlan(plan.Name, plan.Tags, scenarios, plan.SourcePath)));
        }
      }

      return tagFiltered;
    }

    return [.. parsedFeatures
        .Where(pf => string.Equals(pf.Suite.ModName, filter, StringComparison.OrdinalIgnoreCase)
            || FeatureMatchesPath(pf.Plan.SourcePath, filter))];
  }

  private static bool FeatureMatchesPath(string? sourcePath, string filter) {
    if (sourcePath == null) {
      return false;
    }

    string normalizedSource = sourcePath.Replace('\\', '/');
    string normalizedFilter = filter.Replace('\\', '/');

    if (string.Equals(normalizedSource, normalizedFilter, StringComparison.OrdinalIgnoreCase)) {
      return true;
    }

    if (normalizedSource.EndsWith("/" + normalizedFilter, StringComparison.OrdinalIgnoreCase)) {
      return true;
    }

    return string.Equals(Path.GetFileName(sourcePath), filter, StringComparison.OrdinalIgnoreCase);
  }

  private static List<(DiscoveredSuite Suite, FeaturePlan Plan)> ParseFeatures(List<DiscoveredSuite> discoveredSuites) {
    List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsed = new();

    foreach (DiscoveredSuite suite in discoveredSuites) {
      foreach (string featureFile in suite.FeatureFiles) {
        try {
          string featureText = File.ReadAllText(featureFile);
          StringReader reader = new StringReader(featureText);
          Parser parser = new Parser();
          GherkinDocument gherkinDoc = parser.Parse(reader);
          FeaturePlan plan = GherkinAdapter.Adapt(gherkinDoc, featureFile);
          parsed.Add((suite, plan));
        } catch (Exception ex) {
          Log.Error($"pickle: failed to parse {Path.GetFileName(featureFile)}: {ex.Message}");
        }
      }
    }

    return parsed;
  }

  private static List<Assembly> BuildAssemblyList(List<DiscoveredSuite> discoveredSuites) {
    List<Assembly> assemblies = new()
    {
            typeof(SuiteRunner).Assembly,
    };

    Type? vanillaType = Type.GetType("Pickle.Vanilla.UiSteps, Pickle.Vanilla");
    if (vanillaType != null) {
      assemblies.Add(vanillaType.Assembly);
    } else {
      foreach (Assembly a in AppDomain.CurrentDomain.GetAssemblies()) {
        if (a.GetName().Name == "Pickle.Vanilla") {
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
          Log.Warning($"pickle: failed to load steps dll {Path.GetFileName(stepsDll)}: {ex.Message}");
        }
      }
    }

    return assemblies;
  }
}
