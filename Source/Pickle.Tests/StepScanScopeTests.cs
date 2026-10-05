using RimWorks.Pickle.Core.Steps;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class StepScanScopeTests {
  [Theory]
  [InlineData("MultiPlanetFramework")]
  [InlineData("RimWorks.Pickle.Vanilla")]
  [InlineData("SomeMod.Steps")]
  public void CouldHoldSteps_AllowsAModAssembly(string name) {
    Assert.True(StepScanScope.CouldHoldSteps(name));
  }

  [Theory]
  [InlineData("Assembly-CSharp")]
  [InlineData("Assembly-CSharp-firstpass")]
  [InlineData("UnityEngine")]
  [InlineData("UnityEngine.CoreModule")]
  [InlineData("Unity.TextMeshPro")]
  [InlineData("System")]
  [InlineData("System.Core")]
  [InlineData("mscorlib")]
  [InlineData("netstandard")]
  [InlineData("Mono.Security")]
  [InlineData("Microsoft.CSharp")]
  public void CouldHoldSteps_SkipsTheGameAndTheRuntime(string name) {
    Assert.False(StepScanScope.CouldHoldSteps(name));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void CouldHoldSteps_WithNoName_SkipsIt(string? name) {
    Assert.False(StepScanScope.CouldHoldSteps(name));
  }

  [Theory]
  [InlineData("Systemic")]
  [InlineData("UnityEngineering")]
  [InlineData("MonoMod")]
  public void CouldHoldSteps_MatchesWholeSegmentsNotPrefixes(string name) {
    Assert.True(StepScanScope.CouldHoldSteps(name));
  }
}
