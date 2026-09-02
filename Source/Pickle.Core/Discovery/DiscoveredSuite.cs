using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Discovery;

public class DiscoveredSuite {
  public DiscoveredSuite(
      string modName,
      string fixturesDir,
      string writableFixturesDir,
      IReadOnlyList<string> featureFiles,
      IReadOnlyList<string> fixtureFiles,
      IReadOnlyList<string> stepsDlls) {
    ModName = modName;
    FixturesDir = fixturesDir;
    WritableFixturesDir = writableFixturesDir;
    FeatureFiles = featureFiles;
    FixtureFiles = fixtureFiles;
    StepsDlls = stepsDlls;
  }

  public string ModName { get; }

  /// <summary>Committed fixtures, inside the mod.</summary>
  public string FixturesDir { get; }

  /// <summary>Where a newly recorded fixture is written; equal to FixturesDir on a writable install.</summary>
  public string WritableFixturesDir { get; }

  public IReadOnlyList<string> FeatureFiles { get; }

  public IReadOnlyList<string> FixtureFiles { get; }

  public IReadOnlyList<string> StepsDlls { get; }
}
