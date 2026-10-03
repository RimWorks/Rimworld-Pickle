using System.Linq;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class RunEventTests {
  [Fact]
  public void Step_AFailureMessageWithNewlinesStaysOneLine() {
    string line = RunEvent.Step(0, 1, new StepResult("Then", "it works", StepStatus.Failed, 12, "line one\nline two\r\nline three"));

    Assert.DoesNotContain("\n", line);
    Assert.DoesNotContain("\r", line);
    Assert.Contains("line one\\nline two\\r\\nline three", line);
  }

  [Fact]
  public void Step_GherkinTextWithQuotesStaysOneLine() {
    string line = RunEvent.Step(0, 0, new StepResult("Given", "a window \"Dialog_MessageBox\" is answered", StepStatus.Passed, 1));

    Assert.DoesNotContain("\n", line);
    Assert.Contains("\\\"Dialog_MessageBox\\\"", line);
  }

  [Fact]
  public void Step_NoFailureIsJsonNullNotAnEmptyString() {
    string line = RunEvent.Step(0, 0, new StepResult("Given", "it works", StepStatus.Passed, 5));

    Assert.Contains("\"failureMessage\":null", line);
  }

  [Fact]
  public void Step_AnEmptyFailureIsAStringNotNull() {
    string line = RunEvent.Step(0, 0, new StepResult("Given", "it works", StepStatus.Failed, 5, string.Empty));

    Assert.Contains("\"failureMessage\":\"\"", line);
  }

  [Fact]
  public void Step_ArgumentSpansRideAlongWithTheText() {
    StepArgument[] spans = [new StepArgument { Start = 9, Length = 13 }];

    string line = RunEvent.Step(0, 0, new StepResult("Given", "the save \"test-colony\" is loaded", StepStatus.Passed, 8100) { ArgumentSpans = spans });

    Assert.Contains("\"args\":[{\"start\":9,\"length\":13}]", line);
  }

  [Fact]
  public void Step_NoArgumentsIsAnEmptyArrayNotNull() {
    string line = RunEvent.Step(0, 0, new StepResult("Then", "the game is not paused", StepStatus.Passed, 4));

    Assert.Contains("\"args\":[]", line);
  }

  [Fact]
  public void Scenario_CarriesItsIndexOutcomeAndDuration() {
    string line = RunEvent.Scenario(2, "a scenario", "Passed", 120, null);

    Assert.Contains("\"event\":\"scenario\"", line);
    Assert.Contains("\"index\":2", line);
    Assert.Contains("\"outcome\":\"Passed\"", line);
    Assert.Contains("\"durationMs\":120", line);
  }

  [Fact]
  public void RunStarted_AndRunFinished_CarryTheirCounts() {
    Assert.Contains("\"scenarios\":3", RunEvent.RunStarted("a feature", 3));
    Assert.Contains("\"passed\":2", RunEvent.RunFinished(2, 1));
    Assert.Contains("\"failed\":1", RunEvent.RunFinished(2, 1));
  }

  [Fact]
  public void Error_KeepsAMultiLineMessageOnOneLine() {
    string line = RunEvent.Error("Parser errors:\n(2:1): expected something");

    Assert.DoesNotContain("\n", line);
    Assert.Contains("\"event\":\"error\"", line);
  }

  [Fact]
  public void AnyEvent_WithANullNameIsAnEmptyStringNotTheWordNull() {
    Assert.Contains("\"feature\":\"\"", RunEvent.RunStarted(null, 0));
    Assert.Contains("\"name\":\"\"", RunEvent.ScenarioStarted(0, null, 0));
  }

  [Fact]
  public void Document_EndsEveryLineWithANewlineSoAReaderCanSplit() {
    string document = RunEvent.Document([
      RunEvent.RunStarted("f", 1),
      RunEvent.Step(0, 0, new StepResult("Given", "a\nb", StepStatus.Passed, 1)),
      RunEvent.RunFinished(1, 0),
    ]);

    Assert.EndsWith("\n", document);
    string[] lines = [.. document.Split('\n').Where(line => line.Length > 0)];
    Assert.Equal(3, lines.Length);
  }

  [Fact]
  public void Document_IsEmptyForNoLines() {
    Assert.Equal(string.Empty, RunEvent.Document([]));
  }
}
