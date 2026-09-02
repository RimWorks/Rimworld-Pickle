using System.IO;

namespace RimWorks.Pickle.Core.Discovery;

public class SuiteLayout {
  private SuiteLayout(
      string pickleDir, string featuresDir, string fixturesDir, string writableFixturesDir, string assembliesDir) {
    PickleDir = pickleDir;
    FeaturesDir = featuresDir;
    FixturesDir = fixturesDir;
    WritableFixturesDir = writableFixturesDir;
    AssembliesDir = assembliesDir;
  }

  public string PickleDir { get; }

  public string FeaturesDir { get; }

  /// <summary>Committed fixtures, inside the mod. Read-only wherever the mod folder is.</summary>
  public string FixturesDir { get; }

  /// <summary>
  /// Where a newly recorded fixture is written. Falls back to <see cref="FixturesDir"/> when no
  /// writable root is given, which is the plain desktop install.
  /// </summary>
  public string WritableFixturesDir { get; }

  public string AssembliesDir { get; }

  /// <param name="modRoot">The mod folder holding Pickle/.</param>
  /// <param name="writableFixturesRoot">
  /// A directory Pickle may write into. The mod folder is read-only under Docker and on a
  /// Workshop install, so a recorded fixture needs somewhere else to land.
  /// </param>
  public static SuiteLayout FromModRoot(string modRoot, string? writableFixturesRoot = null) {
    string pickleDir = Path.Combine(modRoot, "Pickle");
    string featuresDir = Path.Combine(pickleDir, "Features");
    string fixturesDir = Path.Combine(pickleDir, "Fixtures");
    string assembliesDir = Path.Combine(pickleDir, "Assemblies");

    string writableFixturesDir = string.IsNullOrEmpty(writableFixturesRoot)
        ? fixturesDir
        : Path.Combine(writableFixturesRoot!, Path.GetFileName(modRoot.TrimEnd(Path.DirectorySeparatorChar)));

    return new SuiteLayout(pickleDir, featuresDir, fixturesDir, writableFixturesDir, assembliesDir);
  }
}
