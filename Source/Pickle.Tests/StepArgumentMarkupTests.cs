using System.Collections.Generic;
using RimWorks.Pickle.Core.Steps;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class StepArgumentMarkupTests {
  [Fact]
  public void WrapsEachSpan() {
    List<StepArgument> spans = [new StepArgument { Start = 2, Length = 6 }, new StepArgument { Start = 12, Length = 3 }];

    Assert.Equal("a [\"Wall\"] is [300] tall", StepArgumentMarkup.Apply("a \"Wall\" is 300 tall", spans, "[", "]"));
  }

  [Fact]
  public void OrdersSpansBeforeWrapping() {
    List<StepArgument> spans = [new StepArgument { Start = 12, Length = 3 }, new StepArgument { Start = 2, Length = 6 }];

    Assert.Equal("a [\"Wall\"] is [300] tall", StepArgumentMarkup.Apply("a \"Wall\" is 300 tall", spans, "[", "]"));
  }

  [Fact]
  public void SkipsSpansPastTheEndOfTheText() {
    List<StepArgument> spans = [new StepArgument { Start = 2, Length = 400 }];

    Assert.Equal("a \"Wall\" is 300 tall", StepArgumentMarkup.Apply("a \"Wall\" is 300 tall", spans, "[", "]"));
  }

  [Fact]
  public void FallsBackToQuotedStringsAndNumbersWithoutSpans() {
    Assert.Equal("a [\"Wall\"] is [300] tall", StepArgumentMarkup.Apply("a \"Wall\" is 300 tall", null, "[", "]"));
  }

  [Fact]
  public void LeavesTextWithNoLiteralsAlone() {
    Assert.Equal("the colony is drafted", StepArgumentMarkup.Apply("the colony is drafted", [], "[", "]"));
  }
}
