using System;
using RimWorks.Pickle.Core.Steps;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class StepArgumentSpanTests {
  private static string Slice(string text, StepArgument span) {
    return text.Substring(span.Start, span.Length);
  }

  [Fact]
  public void Spans_StringArgumentKeepsItsQuotes() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("the save {string} is loaded", StepKind.Given, "Test", new[] { typeof(string) }));
    const string text = "the save \"test-colony\" is loaded";

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve(text));

    StepArgument span = Assert.Single(matched.ArgumentSpans);
    Assert.Equal("\"test-colony\"", Slice(text, span));
  }

  [Fact]
  public void Spans_IntArgument() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("I have {int} cukes", StepKind.Given, "Test", new[] { typeof(int) }));
    const string text = "I have 42 cukes";

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve(text));

    Assert.Equal("42", Slice(text, Assert.Single(matched.ArgumentSpans)));
  }

  [Fact]
  public void Spans_FloatArgument() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("altitude is {float}", StepKind.Then, "Test", new[] { typeof(float) }));
    const string text = "altitude is 125.5";

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve(text));

    Assert.Equal("125.5", Slice(text, Assert.Single(matched.ArgumentSpans)));
  }

  [Fact]
  public void Spans_WordArgumentIsFoundEvenThoughItIsUnquoted() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("def {word} exists", StepKind.Then, "Test", new[] { typeof(string) }));
    const string text = "def Human exists";

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve(text));

    Assert.Equal("Human", Slice(text, Assert.Single(matched.ArgumentSpans)));
  }

  [Fact]
  public void Spans_MixedArgumentsStayInOrder() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition(
        "pawn {string} has {int} of thing {string}",
        StepKind.Then,
        "Test",
        new[] { typeof(string), typeof(int), typeof(string) }));
    const string text = "pawn \"Marcus Clayton\" has 3 of thing \"Steel\"";

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve(text));

    Assert.Equal(3, matched.ArgumentSpans.Count);
    Assert.Equal("\"Marcus Clayton\"", Slice(text, matched.ArgumentSpans[0]));
    Assert.Equal("3", Slice(text, matched.ArgumentSpans[1]));
    Assert.Equal("\"Steel\"", Slice(text, matched.ArgumentSpans[2]));
  }

  [Fact]
  public void Spans_RepeatedValueResolvesToTheRightOccurrence() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("copy {word} over {word}", StepKind.When, "Test", new[] { typeof(string), typeof(string) }));
    const string text = "copy Steel over Steel";

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve(text));

    Assert.Equal(2, matched.ArgumentSpans.Count);
    Assert.Equal(5, matched.ArgumentSpans[0].Start);
    Assert.Equal(16, matched.ArgumentSpans[1].Start);
  }

  [Fact]
  public void Spans_RegexFallbackPatternAlsoReportsSpans() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("^I wait (\\d+) ticks$", StepKind.When, "Test", new[] { typeof(int) }));
    const string text = "I wait 500 ticks";

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve(text));

    Assert.Equal("500", Slice(text, Assert.Single(matched.ArgumentSpans)));
  }

  [Fact]
  public void Spans_StepWithNoArgumentsReportsNone() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("the game is not paused", StepKind.Then, "Test", Array.Empty<Type>()));

    MatchedStep matched = Assert.IsType<MatchedStep>(table.Resolve("the game is not paused"));

    Assert.Empty(matched.ArgumentSpans);
  }
}
