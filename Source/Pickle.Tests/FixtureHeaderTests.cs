using System;
using System.IO;
using RimWorks.Pickle.Core.Fixtures;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class FixtureHeaderTests {
  private const string SaveHead = """
      <?xml version="1.0" encoding="utf-8"?>
      <savegame>
        <meta>
          <gameVersion>1.6.4633 rev1257</gameVersion>
          <modIds>
            <li>ludeon.rimworld</li>
            <li>rimworks.pickle</li>
          </modIds>
        </meta>
        <game>
          <scenario>
            <name>Crashlanded</name>
          </scenario>
        </game>
      </savegame>
      """;

  [Fact]
  public void Read_WithASaveHeader_PullsTheFieldsOutOfIt() {
    string path = WriteTemp(SaveHead);
    try {
      FixtureHeader header = FixtureHeader.Read(path);

      Assert.Equal("1.6.4633 rev1257", header.GameVersion);
      Assert.Equal("Crashlanded", header.ScenarioName);
      Assert.Equal(2, header.ModCount);
    } finally {
      File.Delete(path);
    }
  }

  [Fact]
  public void Read_WithAFileThatIsNotASave_ReturnsAnEmptyHeader() {
    string path = WriteTemp("not xml at all");
    try {
      FixtureHeader header = FixtureHeader.Read(path);

      Assert.Null(header.GameVersion);
      Assert.Null(header.ScenarioName);
      Assert.Equal(0, header.ModCount);
    } finally {
      File.Delete(path);
    }
  }

  [Fact]
  public void Read_WithAMissingFile_ReturnsAnEmptyHeader() {
    FixtureHeader header = FixtureHeader.Read(Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString()));

    Assert.Null(header.GameVersion);
    Assert.Equal(0, header.ModCount);
  }

  private static string WriteTemp(string content) {
    string path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString() + ".rws");
    File.WriteAllText(path, content);
    return path;
  }
}
