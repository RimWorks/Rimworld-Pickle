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
  /// <summary>Starts a run for the dashboard's "run" button, refusing while another run or fixture operation is in progress.</summary>
  /// <param name="scope">Which scenarios to run: <c>"selected"</c>, <c>"failed"</c>, or <c>"all"</c>.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
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

  /// <summary>Cancels the active session for the dashboard's abort button.</summary>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task Abort() {
    return Post(() => {
      PickleHttpServer.ActiveSession?.RequestCancel();
      Publish();
    });
  }

  /// <summary>Resumes a paused run. Refused during an autorun, which has nobody to inspect a pause.</summary>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task Continue() {
    return Post(() => {
      if (AutorunState.IsAutorunning) {
        throw new InvalidOperationException("An unattended run cannot pause for inspection.");
      }

      RunnerWindow.Instance.ContinueRun();
    });
  }

  /// <summary>Requests a pause after the current step. Refused during an autorun, which has nobody to inspect a pause.</summary>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task Pause() {
    return Post(() => {
      if (AutorunState.IsAutorunning) {
        throw new InvalidOperationException("An unattended run cannot pause for inspection.");
      }

      RunnerWindow.Instance.ActiveSession?.RequestPause();
    });
  }

  /// <summary>Sets which scenarios the next run covers, without starting one.</summary>
  /// <param name="scope">The run scope: <c>"selected"</c>, <c>"failed"</c>, or <c>"all"</c>.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
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

  /// <summary>Narrows the scenario tree the dashboard shows by search text, mod, and tag.</summary>
  /// <param name="search">Free text to match against scenario and feature names, or <c>null</c> to leave it unchanged.</param>
  /// <param name="mod">The mod to filter to, or <c>null</c> to leave it unchanged.</param>
  /// <param name="tag">A tag to filter by.</param>
  /// <param name="additive">When <c>true</c>, adds <paramref name="tag"/> to the active filters instead of replacing them.</param>
  /// <param name="clearTags">When <c>true</c>, clears every active tag filter before applying <paramref name="tag"/>.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task Filter(string? search, string? mod, string? tag, bool additive = false, bool clearTags = false) {
    return Post(() => {
      if (!AutorunState.IsAutorunning) {
        RunnerWindow.Instance.SetFilter(search, mod, tag, additive, clearTags);
      }
    });
  }

  /// <summary>Selects or deselects one scenario found at an absolute index across every discovered feature.</summary>
  /// <param name="path">The source path of the feature the scenario belongs to.</param>
  /// <param name="index">The scenario's absolute index across every discovered feature.</param>
  /// <param name="on">Whether to select or deselect the scenario.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
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

  /// <summary>Selects or deselects every visible scenario, optionally narrowed to one feature or mod.</summary>
  /// <param name="on">Whether to select or deselect the matching scenarios.</param>
  /// <param name="path">Limits the change to the feature at this source path, or <c>null</c> for every feature.</param>
  /// <param name="mod">Limits the change to this mod, or <c>null</c> for every mod.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
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

  /// <summary>Switches between watch and fast run mode.</summary>
  /// <param name="value">Either <c>"fast"</c> or <c>"watch"</c>.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task SetMode(string value) {
    return Post(() => {
      if (value != "fast" && value != "watch") {
        throw new ArgumentException("Choose watch or fast mode.", nameof(value));
      }

      PickleRunMode.Current = value == "fast" ? PickleRunMode.Mode.Fast : PickleRunMode.Mode.Watch;
      Publish();
    });
  }

  /// <summary>Toggles whether scenarios tagged <c>@wip</c> are included in discovery.</summary>
  /// <param name="on">Whether to include work-in-progress scenarios.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task SetIncludeWip(bool on) {
    return Post(() => {
      IncludeWipState.Enabled = on;
      Publish();
    });
  }

  /// <summary>Toggles the on-screen run pill shown while a scenario plays.</summary>
  /// <param name="on">Whether to show the run pill.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task SetShowRunPill(bool on) {
    return Post(() => {
      RunPillState.Enabled = on;
      Publish();
    });
  }

  /// <summary>Toggles whether a run stops at the first failed scenario instead of continuing.</summary>
  /// <param name="on">Whether to break on the first failure.</param>
  /// <returns>A task that completes once the command has run on the game thread, faulted when it threw.</returns>
  public static Task SetBreakOnFailure(bool on) {
    return Post(() => {
      BreakOnFailureState.Enabled = on;
      Publish();
    });
  }

  /// <summary>Runs a step-like async operation on the driver pump and waits for it to fully finish, not just start.</summary>
  /// <typeparam name="T">The type the work produces.</typeparam>
  /// <param name="work">The operation to run on the main thread.</param>
  /// <returns>A task that completes with <paramref name="work"/>'s result.</returns>
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
