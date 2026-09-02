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

  [Fact]
  public void FromModRoot_WithNoWritableRoot_WritesWhereItReads() {
    SuiteLayout layout = SuiteLayout.FromModRoot("/mods/TestMod");

    Assert.Equal(layout.FixturesDir, layout.WritableFixturesDir);
  }

  [Fact]
  public void FromModRoot_WithWritableRoot_NamesTheFolderAfterTheMod() {
    SuiteLayout layout = SuiteLayout.FromModRoot("/mods/TestMod", "/data/PickleFixtures");

    Assert.Equal(Path.Combine("/mods/TestMod", "Pickle", "Fixtures"), layout.FixturesDir);
    Assert.Equal(Path.Combine("/data/PickleFixtures", "TestMod"), layout.WritableFixturesDir);
  }

  [Fact]
  public void FromModRoot_WithTrailingSeparator_StillNamesTheMod() {
    SuiteLayout layout = SuiteLayout.FromModRoot("/mods/TestMod" + Path.DirectorySeparatorChar, "/data/PickleFixtures");

    Assert.Equal(Path.Combine("/data/PickleFixtures", "TestMod"), layout.WritableFixturesDir);
  }
}
