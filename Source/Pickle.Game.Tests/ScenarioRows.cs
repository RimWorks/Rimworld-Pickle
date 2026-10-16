using System;
using System.IO;
using Gherkin;
using Gherkin.Ast;
using RimWorks.Pickle.Core;
using RimWorks.Pickle.Core.Model;
using Xunit;

namespace RimWorks.Pickle.Game.Tests;

public static class ScenarioRows {
  public static TheoryData<string, int, string> All() {
    TheoryData<string, int, string> rows = new TheoryData<string, int, string>();
    foreach (string path in Directory.GetFiles(FeaturesDir(), "*.feature", SearchOption.AllDirectories)) {
      FeaturePlan plan = Read(path);
      for (int i = 0; i < plan.Scenarios.Count; i++) {
        rows.Add(Path.GetFileName(path), i, plan.Scenarios[i].Name);
      }
    }

    return rows;
  }

  public static FeaturePlan Read(string path) {
    using StreamReader reader = new StreamReader(path);
    GherkinDocument doc = new Parser().Parse(reader);
    return GherkinAdapter.Adapt(doc, path);
  }

  public static string FeaturesDir() {
    return Path.Combine(RepoRoot(), "Pickle", "Features");
  }

  private static string RepoRoot() {
    DirectoryInfo? dir = new DirectoryInfo(AppContext.BaseDirectory);
    while (dir != null) {
      if (Directory.Exists(Path.Combine(dir.FullName, "Pickle", "Features"))) {
        return dir.FullName;
      }

      dir = dir.Parent;
    }

    throw new InvalidOperationException($"no Pickle/Features directory above {AppContext.BaseDirectory}");
  }
}
