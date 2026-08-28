using System.Collections.Generic;

namespace Pickle.Core.Discovery;

public class DiscoveredSuite {
  public DiscoveredSuite(
      string modName,
      string fixturesDir,
      IReadOnlyList<string> featureFiles,
      IReadOnlyList<string> fixtureFiles,
      IReadOnlyList<string> stepsDlls) {
    ModName = modName;
    FixturesDir = fixturesDir;
    FeatureFiles = featureFiles;
    FixtureFiles = fixtureFiles;
    StepsDlls = stepsDlls;
  }

  public string ModName { get; }

  public string FixturesDir { get; }

  public IReadOnlyList<string> FeatureFiles { get; }

  public IReadOnlyList<string> FixtureFiles { get; }

  public IReadOnlyList<string> StepsDlls { get; }
}
