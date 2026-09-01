using System.Collections.Generic;
using RimWorks.Pickle.Core.Model;
using RimWorks.Pickle.Core.Run;

namespace RimWorks.Pickle.Tests;

internal static class ReportWriterTestData {
  public const string JUnitFailureMessage = "a < b && c > d \"quoted\"";

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
                400,
                JUnitFailureMessage,
                ["log line 1", "log line 2"],
                [("screenshot", "/tmp/login-fail.png"), ("note", "some note")],
                [("PlayerState", "hp=10")]),

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
