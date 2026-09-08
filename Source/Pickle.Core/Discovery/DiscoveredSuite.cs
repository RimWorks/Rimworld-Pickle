using System.Collections.Generic;

namespace RimWorks.Pickle.Core.Discovery;

/// <summary>What <see cref="SuiteProbe"/> found under one mod's Pickle folder.</summary>
public class DiscoveredSuite {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="modName">The mod the suite belongs to.</param>
  /// <param name="fixturesDir">Committed fixtures, inside the mod.</param>
  /// <param name="writableFixturesDir">Where a newly recorded fixture is written.</param>
  /// <param name="featureFiles">Every <c>.feature</c> file found under the mod's Features folder.</param>
  /// <param name="fixtureFiles">Every fixture that is not shadowed by a recorded copy.</param>
  /// <param name="shadowedFixtures">Recorded fixtures that hide a committed one of the same name, one line each.</param>
  /// <param name="stepsDlls">Step assemblies found under the mod's Assemblies folder.</param>
  public DiscoveredSuite(
      string modName,
      string fixturesDir,
      string writableFixturesDir,
      IReadOnlyList<string> featureFiles,
      IReadOnlyList<string> fixtureFiles,
      IReadOnlyList<string> shadowedFixtures,
      IReadOnlyList<string> stepsDlls) {
    ModName = modName;
    FixturesDir = fixturesDir;
    WritableFixturesDir = writableFixturesDir;
    FeatureFiles = featureFiles;
    FixtureFiles = fixtureFiles;
    ShadowedFixtures = shadowedFixtures;
    StepsDlls = stepsDlls;
  }

  /// <summary>The mod this suite was discovered in.</summary>
  public string ModName { get; }

  /// <summary>Committed fixtures, inside the mod.</summary>
  public string FixturesDir { get; }

  /// <summary>Where a newly recorded fixture is written; equal to FixturesDir on a writable install.</summary>
  public string WritableFixturesDir { get; }

  /// <summary>Every <c>.feature</c> file found under the mod's Features folder.</summary>
  public IReadOnlyList<string> FeatureFiles { get; }

  /// <summary>Every fixture that is not shadowed by a recorded copy.</summary>
  public IReadOnlyList<string> FixtureFiles { get; }

  /// <summary>Recorded fixtures that hide a committed one of the same name, one line each.</summary>
  public IReadOnlyList<string> ShadowedFixtures { get; }

  /// <summary>Step assemblies found under the mod's Assemblies folder.</summary>
  public IReadOnlyList<string> StepsDlls { get; }
}
