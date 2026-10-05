using RimWorks.Pickle.Core.Steps;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class OptionalTextTests {
  private static StepTable TableFor(string pattern) {
    StepTable table = new StepTable();
    table.Add(new StepDefinition(pattern, StepKind.Then, "Test", new[] { typeof(int) }));
    return table;
  }

  [Theory]
  [InlineData("at least 2 time")]
  [InlineData("at least 2 times")]
  public void OptionalSuffix_MatchesWithAndWithoutIt(string text) {
    Assert.IsType<MatchedStep>(TableFor("at least {int} time(s)").Resolve(text));
  }

  [Fact]
  public void OptionalSuffix_DoesNotMatchTheLiteralParentheses() {
    Assert.IsNotType<MatchedStep>(TableFor("at least {int} time(s)").Resolve("at least 2 time(s)"));
  }
}
