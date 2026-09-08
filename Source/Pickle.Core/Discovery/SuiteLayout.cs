using System.IO;

namespace RimWorks.Pickle.Core.Discovery;

/// <summary>The folders one mod's Pickle install reads from and writes into.</summary>
public class SuiteLayout {
  /// <summary>The folder a mod puts its features and fixtures in, directly under the mod root.</summary>
  public const string DirectoryName = "Pickle";

  private SuiteLayout(
      string pickleDir, string featuresDir, string fixturesDir, string writableFixturesDir, string assembliesDir) {
    PickleDir = pickleDir;
    FeaturesDir = featuresDir;
    FixturesDir = fixturesDir;
    WritableFixturesDir = writableFixturesDir;
    AssembliesDir = assembliesDir;
  }

  /// <summary>The mod's Pickle folder, holding Features, Fixtures and Assemblies.</summary>
  public string PickleDir { get; }

  /// <summary>Where the mod's <c>.feature</c> files live.</summary>
  public string FeaturesDir { get; }

  /// <summary>Committed fixtures, inside the mod. Read-only wherever the mod folder is.</summary>
  public string FixturesDir { get; }

  /// <summary>
  /// Where a newly recorded fixture is written. Falls back to <see cref="FixturesDir"/> when no
  /// writable root is given, which is the plain desktop install.
  /// </summary>
  public string WritableFixturesDir { get; }

  /// <summary>Where a mod ships its step definition assemblies.</summary>
  public string AssembliesDir { get; }

  /// <summary>Builds the layout for one mod from its root folder.</summary>
  /// <param name="modRoot">The mod folder holding Pickle/.</param>
  /// <param name="writableFixturesRoot">
  /// A directory Pickle may write into. The mod folder is read-only under Docker and on a
  /// Workshop install, so a recorded fixture needs somewhere else to land.
  /// </param>
  /// <returns>The resolved layout for <paramref name="modRoot"/>.</returns>
  public static SuiteLayout FromModRoot(string modRoot, string? writableFixturesRoot = null) {
    string pickleDir = Path.Combine(modRoot, DirectoryName);
    string featuresDir = Path.Combine(pickleDir, "Features");
    string fixturesDir = Path.Combine(pickleDir, "Fixtures");
    string assembliesDir = Path.Combine(pickleDir, "Assemblies");

    string writableFixturesDir = string.IsNullOrEmpty(writableFixturesRoot)
        ? fixturesDir
        : Path.Combine(writableFixturesRoot, Path.GetFileName(modRoot.TrimEnd(Path.DirectorySeparatorChar)));

    return new SuiteLayout(pickleDir, featuresDir, fixturesDir, writableFixturesDir, assembliesDir);
  }
}
