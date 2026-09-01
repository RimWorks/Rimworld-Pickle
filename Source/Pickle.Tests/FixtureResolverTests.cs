using System;
using System.Collections.Generic;
using System.IO;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Fixtures;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class FixtureResolverTests {
  [Fact]
  public void Resolve_WithOwnModWin_ReturnsOwnFixturePath() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir = Path.Combine(tempDir, "Pickle");
      string fixturesDir = Path.Combine(pickleDir, "Fixtures");
      Directory.CreateDirectory(fixturesDir);

      string fixturePath = Path.Combine(fixturesDir, "test.rws");
      File.WriteAllText(fixturePath, string.Empty);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      List<DiscoveredSuite> suites = [suite!];
      FixtureResolution resolution = FixtureResolver.Resolve("test", "TestMod", suites);

      Assert.NotNull(resolution.Fixture);
      Assert.Null(resolution.Error);
      Assert.Equal(fixturePath, resolution.Fixture!.FullPath);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Resolve_WithCrossMod_ReturnsCrossModFixturePath() {
    string tempDir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    string tempDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir1 = Path.Combine(tempDir1, "Pickle");
      string fixturesDir1 = Path.Combine(pickleDir1, "Fixtures");
      Directory.CreateDirectory(fixturesDir1);
      File.WriteAllText(Path.Combine(fixturesDir1, "dummy.rws"), string.Empty);

      string pickleDir2 = Path.Combine(tempDir2, "Pickle");
      string fixturesDir2 = Path.Combine(pickleDir2, "Fixtures");
      Directory.CreateDirectory(fixturesDir2);
      string fixturePath = Path.Combine(fixturesDir2, "test.rws");
      File.WriteAllText(fixturePath, string.Empty);

      SuiteLayout layout1 = SuiteLayout.FromModRoot(tempDir1);
      DiscoveredSuite? suite1 = SuiteProbe.Probe("Mod1", layout1);

      SuiteLayout layout2 = SuiteLayout.FromModRoot(tempDir2);
      DiscoveredSuite? suite2 = SuiteProbe.Probe("Mod2", layout2);

      List<DiscoveredSuite> suites = [suite1!, suite2!];
      FixtureResolution resolution = FixtureResolver.Resolve("test", "Mod1", suites);

      Assert.NotNull(resolution.Fixture);
      Assert.Null(resolution.Error);
      Assert.Equal(fixturePath, resolution.Fixture!.FullPath);
    } finally {
      Directory.Delete(tempDir1, true);
      Directory.Delete(tempDir2, true);
    }
  }

  [Fact]
  public void Resolve_WithDuplicateInOtherMods_ReturnsError() {
    string tempDir1 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    string tempDir2 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    string tempDir3 = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir1 = Path.Combine(tempDir1, "Pickle");
      string fixturesDir1 = Path.Combine(pickleDir1, "Fixtures");
      Directory.CreateDirectory(fixturesDir1);
      File.WriteAllText(Path.Combine(fixturesDir1, "dummy.rws"), string.Empty);

      string pickleDir2 = Path.Combine(tempDir2, "Pickle");
      string fixturesDir2 = Path.Combine(pickleDir2, "Fixtures");
      Directory.CreateDirectory(fixturesDir2);
      File.WriteAllText(Path.Combine(fixturesDir2, "test.rws"), string.Empty);

      string pickleDir3 = Path.Combine(tempDir3, "Pickle");
      string fixturesDir3 = Path.Combine(pickleDir3, "Fixtures");
      Directory.CreateDirectory(fixturesDir3);
      File.WriteAllText(Path.Combine(fixturesDir3, "test.rws"), string.Empty);

      SuiteLayout layout1 = SuiteLayout.FromModRoot(tempDir1);
      DiscoveredSuite? suite1 = SuiteProbe.Probe("Mod1", layout1);

      SuiteLayout layout2 = SuiteLayout.FromModRoot(tempDir2);
      DiscoveredSuite? suite2 = SuiteProbe.Probe("Mod2", layout2);

      SuiteLayout layout3 = SuiteLayout.FromModRoot(tempDir3);
      DiscoveredSuite? suite3 = SuiteProbe.Probe("Mod3", layout3);

      List<DiscoveredSuite> suites = [suite1!, suite2!, suite3!];
      FixtureResolution resolution = FixtureResolver.Resolve("test", "Mod1", suites);

      Assert.Null(resolution.Fixture);
      Assert.NotNull(resolution.Error);
      Assert.Equal(FixtureErrorKind.Duplicate, resolution.Error!.Kind);
      Assert.Contains("Mod2", resolution.Error!.Message);
      Assert.Contains("Mod3", resolution.Error!.Message);
    } finally {
      Directory.Delete(tempDir1, true);
      Directory.Delete(tempDir2, true);
      Directory.Delete(tempDir3, true);
    }
  }

  [Fact]
  public void Resolve_WithNotFound_ReturnsNotFoundError() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir = Path.Combine(tempDir, "Pickle");
      string fixturesDir = Path.Combine(pickleDir, "Fixtures");
      Directory.CreateDirectory(fixturesDir);

      File.WriteAllText(Path.Combine(fixturesDir, "other.rws"), string.Empty);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      List<DiscoveredSuite> suites = [suite!];
      FixtureResolution resolution = FixtureResolver.Resolve("notfound", "TestMod", suites);

      Assert.Null(resolution.Fixture);
      Assert.NotNull(resolution.Error);
      Assert.Equal(FixtureErrorKind.NotFound, resolution.Error!.Kind);
      Assert.Contains("other", resolution.Error!.Message);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Resolve_WithCaseInsensitiveMatch_ReturnsFixture() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir = Path.Combine(tempDir, "Pickle");
      string fixturesDir = Path.Combine(pickleDir, "Fixtures");
      Directory.CreateDirectory(fixturesDir);

      string fixturePath = Path.Combine(fixturesDir, "TestFixture.rws");
      File.WriteAllText(fixturePath, string.Empty);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      List<DiscoveredSuite> suites = [suite!];
      FixtureResolution resolution = FixtureResolver.Resolve("testfixture", "TestMod", suites);

      Assert.NotNull(resolution.Fixture);
      Assert.Null(resolution.Error);
      Assert.Equal(fixturePath, resolution.Fixture!.FullPath);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }
}
