using RimWorks.Pickle.Core.Reports;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class EvidenceNameTests {
  private const string LongFeature = "SkillIcons loads after its dependencies and declares its settings shortcut";

  private const string LongScenario =
      "the mod loads after Harmony, Vanilla Skills Expanded and Alpha Skills";

  [Fact]
  public void Stem_ShortNamePassesThroughWithoutAHash() {
    Assert.Equal("a-feature--a-scenario", EvidenceName.Stem("a feature", "a scenario"));
  }

  [Fact]
  public void Stem_ReplacesCharactersAPathCannotHold() {
    Assert.Equal("a-b--c-d-e", EvidenceName.Stem("a/b", "c:d e"));
  }

  [Fact]
  public void Stem_CapsALongNameAtTheLimitPlusAHashSuffix() {
    string stem = EvidenceName.Stem(LongFeature, LongScenario);

    Assert.Equal(EvidenceName.MaxStemLength + 9, stem.Length);
  }

  [Fact]
  public void Stem_KeepsTheWorstCaseScreenshotPathUnderTheWindowsBudget() {
    string filename = EvidenceName.Stem(LongFeature, LongScenario) + "--step1.png";

    Assert.True(filename.Length <= 100, $"filename was {filename.Length} characters: {filename}");
  }

  [Fact]
  public void Stem_TwoNamesSharingTheirFirstSixtyCharactersStayDistinct() {
    string first = EvidenceName.Stem(LongFeature, LongScenario + " and Work Tab");
    string second = EvidenceName.Stem(LongFeature, LongScenario + " and Dubs Bad Hygiene");

    Assert.StartsWith(first.Substring(0, EvidenceName.MaxStemLength), second);
    Assert.NotEqual(first, second);
  }

  [Fact]
  public void Stem_IsStableAcrossCallsSoAFilmFolderResolvesTheSameEveryFrame() {
    Assert.Equal(
        EvidenceName.Stem(LongFeature, LongScenario),
        EvidenceName.Stem(LongFeature, LongScenario));
  }

  [Fact]
  public void Stem_TreatsANullNameAsEmpty() {
    Assert.Equal("--a-scenario", EvidenceName.Stem(null, "a scenario"));
  }
}
