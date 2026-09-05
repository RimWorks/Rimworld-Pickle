using System;
using System.Collections.Generic;
using RimWorks.Pickle.Core.Steps;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class StepTableTests {
  [Fact]
  public void Resolve_IntParameterMatches() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("I have {int} cukes", StepKind.Given, "Test", new[] { typeof(int) });
    table.Add(def);

    StepResolution resolution = table.Resolve("I have 42 cukes");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Single(matched.Args);
    Assert.Equal(42, matched.Args[0]);
  }

  [Fact]
  public void Resolve_StringParameterUnquotes() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("I click {string}", StepKind.When, "Test", new[] { typeof(string) });
    table.Add(def);

    StepResolution resolution = table.Resolve("I click \"OK\"");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Single(matched.Args);
    Assert.Equal("OK", matched.Args[0]);
  }

  [Fact]
  public void Resolve_FloatParameter() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("price is {float}", StepKind.When, "Test", new[] { typeof(float) });
    table.Add(def);

    StepResolution resolution = table.Resolve("price is 19.99");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Single(matched.Args);
    Assert.Equal(19.99f, (float)matched.Args[0]!, 2);
  }

  [Fact]
  public void Resolve_WordParameter() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("I am {word}", StepKind.Given, "Test", new[] { typeof(string) });
    table.Add(def);

    StepResolution resolution = table.Resolve("I am happy");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Single(matched.Args);
    Assert.Equal("happy", matched.Args[0]);
  }

  [Fact]
  public void Resolve_RegexFallback() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("^I wait (\\d+) ticks$", StepKind.When, "Test", new[] { typeof(int) });
    table.Add(def);

    StepResolution resolution = table.Resolve("I wait 5 ticks");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Single(matched.Args);
    Assert.Equal(5, matched.Args[0]);
  }

  [Fact]
  public void Resolve_AmbiguousDefinitions() {
    StepTable table = new StepTable();
    StepDefinition def1 = new StepDefinition("I do something", StepKind.When, "FirstSource", Array.Empty<Type>());
    StepDefinition def2 = new StepDefinition("I do something", StepKind.When, "SecondSource", Array.Empty<Type>());
    table.Add(def1);
    table.Add(def2);

    StepResolution resolution = table.Resolve("I do something");

    Assert.IsType<AmbiguousStep>(resolution);
    AmbiguousStep ambiguous = (AmbiguousStep)resolution;
    Assert.Equal(2, ambiguous.Matches.Count);
    Assert.Contains(def1, ambiguous.Matches);
    Assert.Contains(def2, ambiguous.Matches);
  }

  [Fact]
  public void Resolve_UndefinedRetursSkeleton() {
    StepTable table = new StepTable();

    StepResolution resolution = table.Resolve("unmatched step text");

    Assert.IsType<UndefinedStep>(resolution);
    UndefinedStep undefined = (UndefinedStep)resolution;
    Assert.NotEmpty(undefined.Skeleton);
    Assert.Contains("[When", undefined.Skeleton);
  }

  [Fact]
  public void Resolve_KeywordAgnostic() {
    StepTable table = new StepTable();
    StepDefinition givenDef = new StepDefinition("user is logged in", StepKind.Given, "Test", Array.Empty<Type>());
    table.Add(givenDef);

    StepResolution resolution = table.Resolve("user is logged in");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Equal(givenDef, matched.Definition);
  }

  [Fact]
  public void Resolve_MultipleParameters() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("{word} buys {int} items for {float}", StepKind.When, "Test", new[] { typeof(string), typeof(int), typeof(float) });
    table.Add(def);

    StepResolution resolution = table.Resolve("Alice buys 3 items for 29.99");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Equal(3, matched.Args.Count);
    Assert.Equal("Alice", matched.Args[0]);
    Assert.Equal(3, matched.Args[1]);
    Assert.Equal(29.99f, (float)matched.Args[2]!, 2);
  }

  [Fact]
  public void Resolve_StringParameterSingleQuotes() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("I click {string}", StepKind.When, "Test", new[] { typeof(string) });
    table.Add(def);

    StepResolution resolution = table.Resolve("I click 'Cancel'");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Equal("Cancel", matched.Args[0]);
  }

  [Fact]
  public void Resolve_NegativeNumbers() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("temperature is {int}", StepKind.When, "Test", new[] { typeof(int) });
    table.Add(def);

    StepResolution resolution = table.Resolve("temperature is -5");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Equal(-5, matched.Args[0]);
  }

  [Fact]
  public void Resolve_NegativeFloats() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("balance is {float}", StepKind.When, "Test", new[] { typeof(float) });
    table.Add(def);

    StepResolution resolution = table.Resolve("balance is -3.14");

    Assert.IsType<MatchedStep>(resolution);
    MatchedStep matched = (MatchedStep)resolution;
    Assert.Equal(-3.14f, (float)matched.Args[0]!, 2);
  }

  [Fact]
  public void Resolve_OptionalTextInPattern() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("I have {int} cuke(s)", StepKind.When, "Test", new[] { typeof(int) });
    table.Add(def);

    StepResolution resolution1 = table.Resolve("I have 1 cuke");
    Assert.IsType<MatchedStep>(resolution1);
    MatchedStep matched1 = (MatchedStep)resolution1;
    Assert.Equal(1, matched1.Args[0]);

    StepResolution resolution2 = table.Resolve("I have 5 cukes");
    Assert.IsType<MatchedStep>(resolution2);
    MatchedStep matched2 = (MatchedStep)resolution2;
    Assert.Equal(5, matched2.Args[0]);
  }

  [Fact]
  public void Resolve_AlternationInPattern() {
    StepTable table = new StepTable();
    StepDefinition def = new StepDefinition("I see a red/blue box", StepKind.When, "Test", Array.Empty<Type>());
    table.Add(def);

    StepResolution resolution1 = table.Resolve("I see a red box");
    Assert.IsType<MatchedStep>(resolution1);

    StepResolution resolution2 = table.Resolve("I see a blue box");
    Assert.IsType<MatchedStep>(resolution2);
  }

  [Fact]
  public void Definitions_ListsEveryAddedDefinitionInOrder() {
    StepTable table = new StepTable();
    StepDefinition first = new StepDefinition("I have {int} cukes", StepKind.Given, "Test", [typeof(int)]);
    StepDefinition second = new StepDefinition("I eat one", StepKind.When, "Test", Array.Empty<Type>());
    table.Add(first);
    table.Add(second);

    Assert.Equal([first, second], table.Definitions);
  }

  [Fact]
  public void Definitions_IncludesADefinitionAddedAfterAResolve() {
    StepTable table = new StepTable();
    table.Add(new StepDefinition("I eat one", StepKind.When, "Test", Array.Empty<Type>()));
    _ = table.Resolve("I eat one");

    StepDefinition late = new StepDefinition("the save {string} is loaded", StepKind.Given, "Pickle engine", [typeof(string)]);
    table.Add(late);

    Assert.Contains(late, table.Definitions);
    Assert.Equal(2, table.Definitions.Count);
  }

  [Fact]
  public void Definitions_IsEmptyForANewTable() {
    Assert.Empty(new StepTable().Definitions);
  }
}
