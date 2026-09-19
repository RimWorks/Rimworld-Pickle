namespace RimWorks.Pickle.Patching;

/// <summary>
/// One patching library. A backend expresses Pickle's hooks in its own terms, so the
/// core never references Harmony or Concord.
/// </summary>
public interface IPatchBackend {
  /// <summary>Name used in the log line that reports which backend won.</summary>
  string Name { get; }

  /// <summary>Applies every Pickle hook. Called once, on the winning backend only.</summary>
  void Apply();

  /// <summary>Hooks that must land before RimWorld applies XML patches at load.</summary>
  void ApplyEarly();

  /// <summary>
  /// Reports whether this backend runs what it accepts. Called at most once per backend, before
  /// <see cref="Apply"/>. The default way to answer is to hook <see cref="PatchProbe.Target"/> and
  /// call <see cref="PatchProbe.Fired"/>; a library that publishes its own readiness flag can read
  /// that instead.
  /// </summary>
  /// <returns><c>true</c> when the backend's patches run.</returns>
  bool Probe();
}
