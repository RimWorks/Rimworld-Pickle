using RimWorks.Pickle.Core.Ui;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class WindowSuppressionRuleTests {
  [Fact]
  public void EverythingOpensWhileSuppressionIsOff() {
    Assert.True(WindowSuppressionRule.Allows(active: false, "SomeOtherMod"));
    Assert.True(WindowSuppressionRule.Allows(active: false, "RimWorks.Pickle"));
  }

  [Fact]
  public void AForeignWindowIsDroppedWhileSuppressionIsOn() {
    Assert.False(WindowSuppressionRule.Allows(active: true, "SomeOtherMod"));
  }

  [Fact]
  public void TheRunnerSOwnWindowsAlwaysOpen() {
    Assert.True(WindowSuppressionRule.Allows(active: true, "RimWorks.Pickle"));
    Assert.True(WindowSuppressionRule.Allows(active: true, "RimWorks.Pickle.Vanilla"));
  }

  // The runner's own windows are spared by prefix. A plain Contains would also spare any mod
  // whose assembly name merely includes the runner's, and that mod's windows are exactly what
  // a cleared screen is meant to drop.
  [Fact]
  public void AModWhoseNameMerelyContainsTheRunnerSIsStillForeign() {
    Assert.False(WindowSuppressionRule.Allows(active: true, "NotRimWorks.Pickle"));
  }

  [Fact]
  public void AWindowWhoseAssemblyCannotBeReadIsLetThrough() {
    Assert.True(WindowSuppressionRule.Allows(active: true, null));
    Assert.False(WindowSuppressionRule.IsOwn(null));
  }
}
