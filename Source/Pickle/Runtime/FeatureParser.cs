using System;
using System.Collections.Generic;
using System.IO;
using Gherkin;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>Parses every feature a suite ships and drops the ones that cannot run.</summary>
public static class FeatureParser {
  public static List<(DiscoveredSuite Suite, FeaturePlan Plan)> ParseAll(List<DiscoveredSuite> suites) {
    List<(DiscoveredSuite Suite, FeaturePlan Plan)> parsed = [];

    foreach (DiscoveredSuite suite in suites) {
      foreach (string featureFile in suite.FeatureFiles) {
        FeaturePlan? plan = ParseOne(featureFile);
        if (plan != null) {
          parsed.Add((suite, plan));
        }
      }
    }

    return parsed;
  }

  private static FeaturePlan? ParseOne(string featureFile) {
    string fileName = Path.GetFileName(featureFile);

    try {
      Parser parser = new Parser();
      using StringReader reader = new StringReader(File.ReadAllText(featureFile));
      FeaturePlan plan = GherkinAdapter.Adapt(parser.Parse(reader), featureFile);

      IReadOnlyList<string> problems = QuickstartTag.Problems(plan);
      if (problems.Count == 0) {
        return plan;
      }

      // One bad feature must not take the suite down, so this drops the file and keeps going.
      foreach (string problem in problems) {
        Log.Error("pickle: {FileName} cannot run: {Problem}", [fileName, problem]);
      }

      return null;
    } catch (Exception ex) {
      Log.Error(ex, $"pickle: failed to parse {fileName}");
      return null;
    }
  }
}
