using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using RimWorks.Pickle.Core.Fixtures;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class FixtureCatalogTests {
  [Theory]
  [InlineData("")]
  [InlineData(" ")]
  [InlineData("../outside")]
  [InlineData("..\\outside")]
  [InlineData("/tmp/outside")]
  [InlineData("C:\\outside")]
  [InlineData("..")]
  [InlineData("name ")]
  public void Fixture_names_cannot_escape_the_selected_directory(string name) {
    Assert.Throws<ArgumentException>(() => FixtureCatalog.PathForName("fixtures", name));
  }

  [Fact]
  public void Fixture_names_keep_spaces_and_unicode_inside_the_directory() {
    Assert.Equal(Path.Combine("fixtures", "colony café.rws"), FixtureCatalog.PathForName("fixtures", "colony café"));
  }

  [Fact]
  public void Read_WithOnlyCommittedFixtures_MarksThemCommitted() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      Directory.CreateDirectory(tempDir);
      File.WriteAllText(Path.Combine(tempDir, "one-planet.rws"), "hello");

      List<FixtureEntry> entries = FixtureCatalog.Read(tempDir, tempDir);

      FixtureEntry entry = Assert.Single(entries);
      Assert.Equal("one-planet", entry.Name);
      Assert.False(entry.IsRecorded);
      Assert.Null(entry.ShadowedPath);
      Assert.False(entry.IsShadowed);
      Assert.Equal(5, entry.SizeBytes);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Read_WhenARecordedFixtureSharesAName_ListsBothAndMarksTheLoser() {
    string tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    try {
      string committedDir = Path.Combine(tempDir, "committed");
      string writableDir = Path.Combine(tempDir, "writable");
      Directory.CreateDirectory(committedDir);
      Directory.CreateDirectory(writableDir);

      File.WriteAllText(Path.Combine(committedDir, "one-planet.rws"), string.Empty);
      File.WriteAllText(Path.Combine(writableDir, "one-planet.rws"), string.Empty);
      File.WriteAllText(Path.Combine(writableDir, "only-recorded.rws"), string.Empty);

      List<FixtureEntry> entries = FixtureCatalog.Read(committedDir, writableDir);

      Assert.Equal(3, entries.Count);

      FixtureEntry winner = entries.Single(e => e.Name == "one-planet" && !e.IsShadowed);
      Assert.True(winner.IsRecorded);
      Assert.Equal(Path.Combine(writableDir, "one-planet.rws"), winner.FullPath);
      Assert.Equal(Path.Combine(committedDir, "one-planet.rws"), winner.ShadowedPath);

      FixtureEntry loser = entries.Single(e => e.Name == "one-planet" && e.IsShadowed);
      Assert.False(loser.IsRecorded);
      Assert.Equal(Path.Combine(committedDir, "one-planet.rws"), loser.FullPath);

      FixtureEntry recordedOnly = entries.Single(e => e.Name == "only-recorded");
      Assert.True(recordedOnly.IsRecorded);
      Assert.False(recordedOnly.IsShadowed);
      Assert.Null(recordedOnly.ShadowedPath);
    } finally {
      Directory.Delete(tempDir, true);
    }
  }

  [Fact]
  public void Read_WithNoDirectories_ReturnsNothing() {
    string missing = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    Assert.Empty(FixtureCatalog.Read(missing, missing));
  }
}
