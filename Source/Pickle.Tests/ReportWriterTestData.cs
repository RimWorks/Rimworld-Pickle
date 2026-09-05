using System.Collections.Generic;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Tests;

internal static class ReportWriterTestData {
  public const string JUnitFailureMessage = "a < b && c > d \"quoted\"";

  /// <summary>One passed-on-retry scenario and one that failed every attempt.</summary>
  public static List<ScenarioResult> BuildFlakyRun() {
    return
    [
        new ScenarioResult(
                "flaky checkout",
                "Checkout",
                new TagSet(["@retry:2"]),
                ScenarioOutcome.Passed,
                [new StepResult("Given", "I have items in my cart", StepStatus.Passed, 90)],
                180) {
              Attempts = 3,
              FailedAttempts = [(1, "cart was empty"), (2, null)],
            },

            new ScenarioResult(
                "stubbornly broken",
                "Checkout",
                new TagSet(["@retry:1"]),
                ScenarioOutcome.Failed,
                [new StepResult("Given", "I have items in my cart", StepStatus.Failed, 40, "still empty")],
                40) {
              FailureMessage = "still empty",
              Attempts = 2,
              FailedAttempts = [(1, "still empty")],
            },
        ];
  }

  /// <summary>One scenario that drove ticks and one that never loaded a world.</summary>
  public static List<ScenarioResult> BuildTickCostRun() {
    return
    [
        new ScenarioResult(
                "waits out a thousand ticks",
                "Sim",
                new TagSet([]),
                ScenarioOutcome.Passed,
                [new StepResult("When", "I wait 1000 ticks", StepStatus.Passed, 900)],
                900) {
              TickCost = (1000, 3.25, 41.5),
            },

            new ScenarioResult(
                "reads a def at the main menu",
                "Sim",
                new TagSet([]),
                ScenarioOutcome.Passed,
                [new StepResult("Then", "def \"Human\" exists", StepStatus.Passed, 4)],
                4),
        ];
  }

  public static List<ScenarioResult> BuildTwoFeatureRun() {
    return
    [
        new ScenarioResult(
                "successful login",
                "Login",
                new TagSet([]),
                ScenarioOutcome.Passed,
                [
                    new StepResult("Given", "I am on the login page", StepStatus.Passed, 100),
                    new StepResult("When", "I log in with valid credentials", StepStatus.Passed, 150),
                ],
                250),

            new ScenarioResult(
                "failed login",
                "Login",
                new TagSet([]),
                ScenarioOutcome.Failed,
                [
                    new StepResult("Given", "I am on the login page", StepStatus.Passed, 100),
                    new StepResult("When", "I log in with bad credentials", StepStatus.Failed, 300, JUnitFailureMessage),
                ],
                400) {
              FailureMessage = JUnitFailureMessage,
              LogTail = ["log line 1", "log line 2"],
              Attachments = [("screenshot", "/tmp/login-fail.png"), ("note", "some note")],
              StateDumps = [("PlayerState", "hp=10")],
            },

            new ScenarioResult(
                "guest checkout",
                "Checkout",
                new TagSet([]),
                ScenarioOutcome.Passed,
                [
                    new StepResult("Given", "I have items in my cart", StepStatus.Passed, 120),
                ],
                120),

            new ScenarioResult(
                "skipped scenario",
                "Checkout",
                new TagSet(["@wip"]),
                ScenarioOutcome.Skipped,
                [
                    new StepResult("Given", "I have items in my cart", StepStatus.Skipped, 0),
                    new StepResult("When", "I check out", StepStatus.Skipped, 0),
                ],
                0),
        ];
  }
}
