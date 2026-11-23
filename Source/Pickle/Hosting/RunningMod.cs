namespace RimWorks.Pickle.Hosting;

/// <summary>One mod the host's game is running.</summary>
public sealed class RunningMod {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="name">The mod's display name.</param>
  /// <param name="packageId">The package id the host tells this copy apart by.</param>
  /// <param name="packageIdPlayerFacing">The package id as the mod's author wrote it.</param>
  /// <param name="rootDir">The mod's root folder.</param>
  public RunningMod(string name, string packageId, string packageIdPlayerFacing, string rootDir) {
    Name = name;
    PackageId = packageId;
    PackageIdPlayerFacing = packageIdPlayerFacing;
    RootDir = rootDir;
  }

  /// <summary>The mod's display name.</summary>
  public string Name { get; }

  /// <summary>The package id the host tells this copy apart by. It can carry a postfix the author
  /// never wrote, such as one for a second copy of the same mod.</summary>
  public string PackageId { get; }

  /// <summary>The package id as the mod's author wrote it.</summary>
  public string PackageIdPlayerFacing { get; }

  /// <summary>The mod's root folder.</summary>
  public string RootDir { get; }
}
