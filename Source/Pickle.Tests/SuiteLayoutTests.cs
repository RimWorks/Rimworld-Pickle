using System.IO;
using RimWorks.Pickle.Core.Discovery;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class SuiteLayoutTests {
  [Fact]
  public void FromModRoot_ComposesPathsCorrectly() {
    string modRoot = "/home/user/rimworld/mods/TestMod";

    SuiteLayout layout = SuiteLayout.FromModRoot(modRoot);

    Assert.Equal(Path.Combine(modRoot, "Pickle", "Features"), layout.FeaturesDir);
    Assert.Equal(Path.Combine(modRoot, "Pickle", "Fixtures"), layout.FixturesDir);
    Assert.Equal(Path.Combine(modRoot, "Pickle", "Assemblies"), layout.AssembliesDir);
  }

  [Fact]
  public void FromModRoot_UsesPathCombine() {
    string modRoot = "TestMod";

    SuiteLayout layout = SuiteLayout.FromModRoot(modRoot);

    string expectedFeatures = Path.Combine(modRoot, "Pickle", "Features");
    Assert.Equal(expectedFeatures, layout.FeaturesDir);
  }
}
