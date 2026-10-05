using System.IO;
using RimWorks.Pickle.Core.Fixtures;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class SaveFilePathTests {
  private static readonly string Saves = Path.Combine("home", "saves");

  [Fact]
  public void Resolve_BareName_ReadsFromTheSavedGamesFolder() {
    Assert.Equal(Path.Combine(Saves, "Nelims-tribe.rws"), SaveFilePath.Resolve("Nelims-tribe", Saves));
  }

  [Fact]
  public void Resolve_FileName_KeepsTheExtensionItWasGiven() {
    Assert.Equal(Path.Combine(Saves, "Nelims-tribe.rws"), SaveFilePath.Resolve("Nelims-tribe.rws", Saves));
  }

  [Fact]
  public void Resolve_RootedPath_IsTakenAsWritten() {
    string rooted = Path.Combine(Path.GetTempPath(), "scratch.rws");

    Assert.Equal(rooted, SaveFilePath.Resolve(rooted, Saves));
  }

  [Fact]
  public void Resolve_RootedPathWithNoExtension_StillGetsRws() {
    string rooted = Path.Combine(Path.GetTempPath(), "scratch");

    Assert.Equal(rooted + ".rws", SaveFilePath.Resolve(rooted, Saves));
  }

  [Fact]
  public void Resolve_TrimsSurroundingSpace() {
    Assert.Equal(Path.Combine(Saves, "a.rws"), SaveFilePath.Resolve("  a  ", Saves));
  }

  [Theory]
  [InlineData(null)]
  [InlineData("")]
  [InlineData("   ")]
  public void Resolve_WithNoReference_IsEmpty(string? reference) {
    Assert.Equal(string.Empty, SaveFilePath.Resolve(reference, Saves));
  }
}
