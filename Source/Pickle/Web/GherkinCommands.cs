using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using RimWorks.Pickle.Run;
using RimWorks.Pickle.Runtime;

namespace RimWorks.Pickle.Web;

/// <summary>Runs Gherkin that came from no file, reporting each step as it finishes.</summary>
public static class GherkinCommands {
  private const int DrainIntervalMs = 25;

  private const int HistoryLimit = 20;

  private static readonly object HistoryLock = new object();
  private static readonly List<Attempt> History = new List<Attempt>();

  private static int counter;

  /// <summary>Every one-off run this session kept, newest first, with the newest one's live events.</summary>
  /// <returns>A JSON object holding <c>active</c> and a <c>runs</c> array.</returns>
  public static string Recent() {
    lock (HistoryLock) {
      List<string> runs = new List<string>();
      for (int i = History.Count - 1; i >= 0; i--) {
        Attempt attempt = History[i];
        runs.Add("{\"id\":" + attempt.Id
            + ",\"startedAt\":" + Json.Quote(attempt.StartedAt)
            + ",\"source\":" + Json.Quote(attempt.Source)
            + ",\"active\":" + (attempt.Active ? "true" : "false")
            + ",\"events\":[" + string.Join(",", [.. attempt.Events]) + "]}");
      }

      bool running = History.Count > 0 && History[History.Count - 1].Active;
      return "{\"active\":" + (running ? "true" : "false")
          + ",\"runs\":[" + string.Join(",", [.. runs]) + "]}";
    }
  }

  /// <summary>
  /// Parses and runs the text, handing every event to <paramref name="emit"/> as it happens. Never
  /// call this on the main thread: it blocks, and the run it waits for needs that thread to finish.
  /// </summary>
  /// <param name="gherkin">The Gherkin source to run.</param>
  /// <param name="emit">Called on the calling thread with one JSON line per event.</param>
  public static void Stream(string? gherkin, Action<string> emit) {
    ConcurrentQueue<string> events = new ConcurrentQueue<string>();

    Attempt attempt = new Attempt {
      Source = gherkin ?? string.Empty,
      StartedAt = DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture),
      Active = true,
    };

    lock (HistoryLock) {
      attempt.Id = ++counter;
      History.Add(attempt);
      while (History.Count > HistoryLimit) {
        History.RemoveAt(0);
      }
    }

    void Keep(string line) {
      lock (HistoryLock) {
        attempt.Events.Add(line);
      }

      emit(line);
    }

    try {
      Drive(gherkin, events, Keep);
    } finally {
      lock (HistoryLock) {
        attempt.Active = false;
      }
    }
  }

  private static void Drive(string? gherkin, ConcurrentQueue<string> events, Action<string> emit) {

    FeaturePlan plan;
    try {
      plan = FeatureParser.ParseText(gherkin);
    } catch (Exception ex) {
      emit(RunEvent.Error(ex.Message));
      return;
    }

    emit(RunEvent.RunStarted(plan.Name, plan.Scenarios.Count));

    Task<(int Passed, int Failed)> run = RunnerCommands.PostAsync(() => Execute(plan, events));

    while (!run.IsCompleted) {
      Drain(events, emit);
      Thread.Sleep(DrainIntervalMs);
    }

    Drain(events, emit);

    if (run.IsFaulted) {
      emit(RunEvent.Error(Describe(run.Exception)));
      return;
    }

    (int passed, int failed) = run.GetAwaiter().GetResult();
    emit(RunEvent.RunFinished(passed, failed));
  }

  private static async Task<(int Passed, int Failed)> Execute(FeaturePlan plan, ConcurrentQueue<string> events) {
    StepConsole.RefuseWhenBusy();

    (StepTable table, List<Type> stepsTypes, List<DiscoveredSuite> suites) = SuiteRunner.BuildStepEnvironment();
    RunSession session = new RunSession(table, PickleDriver.Instance, suites, stepsTypes);

    int scenarioIndex = 0;
    int emittedSteps = 0;
    ScenarioPlan? announced = null;
    int passed = 0;
    int failed = 0;

    session.OnProgress = () => {
      if (!ReferenceEquals(session.CurrentScenario, announced)) {
        announced = session.CurrentScenario;
        emittedSteps = 0;
        if (announced != null) {
          events.Enqueue(RunEvent.ScenarioStarted(scenarioIndex, session.CurrentScenarioName, announced.Steps.Count));
        }
      }

      IReadOnlyList<StepResult> done = session.CurrentStepResults;
      for (; emittedSteps < done.Count; emittedSteps++) {
        StepResult step = done[emittedSteps];
        events.Enqueue(RunEvent.Step(
            scenarioIndex,
            emittedSteps,
            step.Keyword,
            step.Text,
            step.Status.ToString(),
            (long)step.DurationMs,
            step.FailureMessage));
      }
    };

    void Completed(ScenarioResult result) {
      if (result.Outcome == ScenarioOutcome.Failed) {
        failed++;
      } else if (result.Outcome == ScenarioOutcome.Passed) {
        passed++;
      }

      events.Enqueue(RunEvent.Scenario(
          scenarioIndex,
          result.ScenarioName,
          result.Outcome.ToString(),
          (long)result.DurationMs,
          result.FailureMessage));

      scenarioIndex++;
      announced = null;
      emittedSteps = 0;
    }

    try {
      await session.RunFeature(plan, "dashboard", includeWip: true, onScenarioCompleted: Completed);
    } finally {
      session.OnProgress = null;
    }

    return (passed, failed);
  }

  private static void Drain(ConcurrentQueue<string> events, Action<string> emit) {
    while (events.TryDequeue(out string line)) {
      emit(line);
    }
  }

  private static string Describe(AggregateException? failure) {
    Exception? inner = failure?.GetBaseException();
    return inner?.Message ?? "the run failed";
  }

  private sealed class Attempt {
    public int Id { get; set; }

    public string StartedAt { get; set; } = string.Empty;

    public string Source { get; set; } = string.Empty;

    public bool Active { get; set; }

    public List<string> Events { get; } = new List<string>();
  }
}
