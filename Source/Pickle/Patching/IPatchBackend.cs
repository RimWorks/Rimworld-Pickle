namespace Pickle.Patching;

/// <summary>
/// One patching library. A backend expresses Pickle's hooks in its own terms, so the
/// core never references Harmony or Concord.
/// </summary>
public interface IPatchBackend {
  /// <summary>Name used in the log line that reports which backend won.</summary>
  public string Name { get; }

  /// <summary>Applies every Pickle hook. Called once, on the winning backend only.</summary>
  public void Apply();

  /// <summary>Hooks that must land before RimWorld applies XML patches at load.</summary>
  public void ApplyEarly();
}
