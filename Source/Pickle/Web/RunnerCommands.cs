using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;

namespace RimWorks.Pickle.Web;

/// <summary>
/// Dashboard commands, all marshalled onto the main thread through the driver pump.
/// They touch the runner and RimWorld statics, which the listener thread must not.
/// </summary>
public static class RunnerCommands {
  public static void Run(string scope) {
    Post(() => {
      RunnerWindow runner = RunnerWindow.Instance;
      if (runner.IsRunning) {
        return;
      }

      switch (scope) {
        case "selected":
          runner.RunSelected();
          break;
        case "failed":
          runner.RerunFailed();
          break;
        default:
          _ = runner.RunAllAndWait();
          break;
      }
    });
  }

  public static void Abort() {
    PickleHttpServer.ActiveSession?.RequestCancel();
  }

  public static void Select(string path, int index, bool on) {
    Post(() => {
      RunnerWindow.Instance.SetScenarioSelected(path, index, on);
      RunnerWindow.Instance.PublishSnapshot();
    });
  }

  public static void SelectAll(bool on) {
    Post(() => {
      RunnerWindow runner = RunnerWindow.Instance;
      int scenarioIndex = 0;
      foreach ((_, Core.Model.FeaturePlan plan) in runner.ParsedFeatures) {
        string sourcePath = plan.SourcePath ?? string.Empty;
        for (int i = 0; i < plan.Scenarios.Count; i++) {
          runner.SetScenarioSelected(sourcePath, scenarioIndex + i, on);
        }

        scenarioIndex += plan.Scenarios.Count;
      }

      runner.PublishSnapshot();
    });
  }

  public static void SetMode(string value) {
    PickleRunMode.Current = value == "fast" ? PickleRunMode.Mode.Fast : PickleRunMode.Mode.Watch;
    Post(() => RunnerWindow.Instance.PublishSnapshot());
  }

  public static void SetBreakOnFailure(bool on) {
    BreakOnFailureState.Enabled = on;
    Post(() => RunnerWindow.Instance.PublishSnapshot());
  }

  // DashboardSeed creates the driver on the main thread. Posting before that builds it
  // from the listener thread, which captures the wrong main-thread id.
  private static void Post(System.Action action) {
    if (PickleDriver.Exists) {
      PickleDriver.Post(action);
    }
  }
}
