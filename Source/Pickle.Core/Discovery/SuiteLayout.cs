using System.IO;

namespace Pickle.Core.Discovery;

public class SuiteLayout {
  private SuiteLayout(string pickleDir, string featuresDir, string fixturesDir, string assembliesDir) {
    PickleDir = pickleDir;
    FeaturesDir = featuresDir;
    FixturesDir = fixturesDir;
    AssembliesDir = assembliesDir;
  }

  public string PickleDir { get; }

  public string FeaturesDir { get; }

  public string FixturesDir { get; }

  public string AssembliesDir { get; }

  public static SuiteLayout FromModRoot(string modRoot) {
    string pickleDir = Path.Combine(modRoot, "Pickle");
    string featuresDir = Path.Combine(pickleDir, "Features");
    string fixturesDir = Path.Combine(pickleDir, "Fixtures");
    string assembliesDir = Path.Combine(pickleDir, "Assemblies");

    return new SuiteLayout(pickleDir, featuresDir, fixturesDir, assembliesDir);
  }
}
