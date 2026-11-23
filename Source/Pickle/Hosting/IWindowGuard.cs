namespace RimWorks.Pickle.Hosting;

/// <summary>The host's guard against windows that open outside the runner and block a scenario.</summary>
public interface IWindowGuard {
  /// <summary>Stops dropping those windows, so the game behaves normally again.</summary>
  void EndSuppression();
}
