using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Autorun;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;

namespace RimWorks.Pickle.Web;

/// <summary>
/// Dashboard commands, all marshalled onto the main thread through the driver pump.
/// They touch the runner and RimWorld statics, which the listener thread must not.
/// </summary>
public static class RunnerCommands {
  public static Task Run(string scope) {
    return Post(() => {
      if (AutorunState.IsAutorunning) {
        return;
      }

      RunnerWindow runner = RunnerWindow.Instance;
      if (runner.IsRunning || FixtureCommands.IsBusy) {
        throw new InvalidOperationException("Wait for the current run or fixture operation to finish.");
      }

      runner.RunScope = scope;

      switch (scope) {
        case "selected":
          runner.RunSelected();
          break;
        case "failed":
          runner.RerunFailed();
          break;
        case "all":
          _ = runner.RunAllAndWait();
          break;
        default:
          throw new ArgumentException("Unknown run scope.", nameof(scope));
      }
    });
  }

  public static Task Abort() {
    return Post(() => {
      PickleHttpServer.ActiveSession?.RequestCancel();
      Publish();
    });
  }

  public static Task Continue() {
    return Post(() => {
      if (AutorunState.IsAutorunning) {
        throw new InvalidOperationException("An unattended run cannot pause for inspection.");
      }

      RunnerWindow.Instance.ContinueRun();
    });
  }

  public static Task Pause() {
    return Post(() => {
      if (AutorunState.IsAutorunning) {
        throw new InvalidOperationException("An unattended run cannot pause for inspection.");
      }

      RunnerWindow.Instance.ActiveSession?.RequestPause();
    });
  }

  public static Task SetScope(string scope) {
    return Post(() => {
      if (scope != "all" && scope != "selected" && scope != "failed") {
        throw new ArgumentException("Unknown run scope.", nameof(scope));
      }

      if (!RunnerWindow.Instance.IsRunning && !AutorunState.IsAutorunning) {
        RunnerWindow.Instance.RunScope = scope;
        Publish();
      }
    });
  }

  public static Task Filter(string? search, string? mod, string? tag, bool additive = false, bool clearTags = false) {
    return Post(() => {
      if (!AutorunState.IsAutorunning) {
        RunnerWindow.Instance.SetFilter(search, mod, tag, additive, clearTags);
      }
    });
  }

  public static Task Select(string path, int index, bool on) {
    return Post(() => {
      if (AutorunState.IsAutorunning) {
        return;
      }

      RunnerWindow runner = RunnerWindow.Instance;
      int start = 0;
      foreach ((_, Core.Model.FeaturePlan plan) in runner.ParsedFeatures) {
        if (path == plan.SourcePath && index >= start && index < start + plan.Scenarios.Count) {
          runner.SetScenarioSelected(path, index, on);
          runner.PublishSnapshot();
          return;
        }

        start += plan.Scenarios.Count;
      }

      throw new ArgumentException("Select a discovered scenario.", nameof(index));
    });
  }

  public static Task SelectAll(bool on, string? path = null, string? mod = null) {
    return Post(() => {
      if (AutorunState.IsAutorunning) {
        return;
      }

      RunnerWindow runner = RunnerWindow.Instance;
      int scenarioIndex = 0;
      foreach ((Core.Discovery.DiscoveredSuite suite, Core.Model.FeaturePlan plan) in runner.ParsedFeatures) {
        string sourcePath = plan.SourcePath ?? string.Empty;
        for (int i = 0; i < plan.Scenarios.Count; i++) {
          if ((path == null || path == sourcePath) && (mod == null || mod == suite.ModName)
              && ((path == null && mod == null) || runner.IsScenarioVisible(suite, plan, plan.Scenarios[i]))) {
            runner.SetScenarioSelected(sourcePath, scenarioIndex + i, on);
          }
        }

        scenarioIndex += plan.Scenarios.Count;
      }

      runner.PublishSnapshot();
    });
  }

  public static Task SetMode(string value) {
    return Post(() => {
      if (value != "fast" && value != "watch") {
        throw new ArgumentException("Choose watch or fast mode.", nameof(value));
      }

      PickleRunMode.Current = value == "fast" ? PickleRunMode.Mode.Fast : PickleRunMode.Mode.Watch;
      Publish();
    });
  }

  public static Task SetIncludeWip(bool on) {
    return Post(() => {
      IncludeWipState.Enabled = on;
      Publish();
    });
  }

  public static Task SetShowRunPill(bool on) {
    return Post(() => {
      RunPillState.Enabled = on;
      Publish();
    });
  }

  public static Task SetBreakOnFailure(bool on) {
    return Post(() => {
      BreakOnFailureState.Enabled = on;
      Publish();
    });
  }

  // Post completes when the action returns, which is wrong for a step: it is async and
  // may wait ticks. The driver resolves continuations inline on the main thread, so the
  // await inside work resumes there and completing from it is safe.
  internal static Task<T> PostAsync<T>(Func<Task<T>> work) {
    TaskCompletionSource<T> completion = new TaskCompletionSource<T>();
    if (!PickleDriver.Exists) {
      completion.SetException(new InvalidOperationException("The game is still loading."));
      return completion.Task;
    }

    PickleDriver.Post(async () => {
      try {
        completion.SetResult(await work());
      } catch (Exception ex) {
        completion.SetException(ex);
      }
    });
    return completion.Task;
  }

  private static void Publish() {
    if (AutorunState.IsAutorunning) {
      PickleHttpServer.ActiveSession?.OnProgress?.Invoke();
    } else {
      RunnerWindow.Instance.PublishSnapshot();
    }
  }

  // DashboardSeed creates the driver on the main thread. Posting before that builds it
  // from the listener thread, which captures the wrong main-thread id.
  private static Task Post(Action action) {
    TaskCompletionSource<bool> completion = new TaskCompletionSource<bool>();
    if (!PickleDriver.Exists) {
      completion.SetException(new InvalidOperationException("The game is still loading."));
      return completion.Task;
    }

    PickleDriver.Post(() => {
      try {
        action();
        completion.SetResult(true);
      } catch (Exception ex) {
        completion.SetException(ex);
      }
    });
    return completion.Task;
  }
}
