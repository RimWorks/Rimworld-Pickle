using System;
using System.IO;
using System.Linq;
using Pickle.Core.Discovery;
using Xunit;

namespace Pickle.Tests;

public class SuiteProbeTests {
  [Fact]
  public void Probe_WithAllSubdirs_ReturnsSuiteWithAllFiles() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir = Path.Combine(tempDir, "Pickle");
      string featuresDir = Path.Combine(pickleDir, "Features");
      string fixturesDir = Path.Combine(pickleDir, "Fixtures");
      string assembliesDir = Path.Combine(pickleDir, "Assemblies");

      Directory.CreateDirectory(featuresDir);
      Directory.CreateDirectory(fixturesDir);
      Directory.CreateDirectory(assembliesDir);

      File.WriteAllText(Path.Combine(featuresDir, "test1.feature"), "Feature: Test 1");
      File.WriteAllText(Path.Combine(featuresDir, "test2.feature"), "Feature: Test 2");
      File.WriteAllText(Path.Combine(fixturesDir, "fixture1.rws"), string.Empty);
      File.WriteAllText(Path.Combine(assembliesDir, "steps.dll"), string.Empty);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      Assert.NotNull(suite);
      Assert.Equal("TestMod", suite.ModName);
      Assert.Equal(2, suite.FeatureFiles.Count);
      Assert.Contains(Path.Combine(featuresDir, "test1.feature"), suite.FeatureFiles);
      Assert.Contains(Path.Combine(featuresDir, "test2.feature"), suite.FeatureFiles);
      Assert.Single(suite.FixtureFiles);
      Assert.Contains(Path.Combine(fixturesDir, "fixture1.rws"), suite.FixtureFiles);
      Assert.Single(suite.StepsDlls);
      Assert.Contains(Path.Combine(assembliesDir, "steps.dll"), suite.StepsDlls);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Probe_WithMissingSubdirs_ReturnsEmptyLists() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir = Path.Combine(tempDir, "Pickle");
      Directory.CreateDirectory(pickleDir);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      Assert.NotNull(suite);
      Assert.Empty(suite.FeatureFiles);
      Assert.Empty(suite.FixtureFiles);
      Assert.Empty(suite.StepsDlls);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Probe_WithNoPicleDir_ReturnsNull() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      Directory.CreateDirectory(tempDir);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      Assert.Null(suite);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Probe_WithRecursiveFeatures_FindsAllFiles() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir = Path.Combine(tempDir, "Pickle");
      string featuresDir = Path.Combine(pickleDir, "Features");
      string subDir = Path.Combine(featuresDir, "subdir");

      Directory.CreateDirectory(subDir);

      File.WriteAllText(Path.Combine(featuresDir, "test1.feature"), string.Empty);
      File.WriteAllText(Path.Combine(subDir, "test2.feature"), string.Empty);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      Assert.NotNull(suite);
      Assert.Equal(2, suite.FeatureFiles.Count);
      Assert.Contains(Path.Combine(featuresDir, "test1.feature"), suite.FeatureFiles);
      Assert.Contains(Path.Combine(subDir, "test2.feature"), suite.FeatureFiles);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Probe_ReturnsSortedResults() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string pickleDir = Path.Combine(tempDir, "Pickle");
      string featuresDir = Path.Combine(pickleDir, "Features");
      string fixturesDir = Path.Combine(pickleDir, "Fixtures");
      string assembliesDir = Path.Combine(pickleDir, "Assemblies");

      Directory.CreateDirectory(featuresDir);
      Directory.CreateDirectory(fixturesDir);
      Directory.CreateDirectory(assembliesDir);

      File.WriteAllText(Path.Combine(featuresDir, "z.feature"), string.Empty);
      File.WriteAllText(Path.Combine(featuresDir, "a.feature"), string.Empty);
      File.WriteAllText(Path.Combine(fixturesDir, "z.rws"), string.Empty);
      File.WriteAllText(Path.Combine(fixturesDir, "a.rws"), string.Empty);
      File.WriteAllText(Path.Combine(assembliesDir, "z.dll"), string.Empty);
      File.WriteAllText(Path.Combine(assembliesDir, "a.dll"), string.Empty);

      SuiteLayout layout = SuiteLayout.FromModRoot(tempDir);
      DiscoveredSuite? suite = SuiteProbe.Probe("TestMod", layout);

      Assert.NotNull(suite);
      Assert.Equal([Path.Combine(featuresDir, "a.feature"), Path.Combine(featuresDir, "z.feature")], suite.FeatureFiles);
      Assert.Equal([Path.Combine(fixturesDir, "a.rws"), Path.Combine(fixturesDir, "z.rws")], suite.FixtureFiles);
      Assert.Equal([Path.Combine(assembliesDir, "a.dll"), Path.Combine(assembliesDir, "z.dll")], suite.StepsDlls);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }
}
