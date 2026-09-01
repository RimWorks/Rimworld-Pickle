using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;
using Verse;

namespace RimWorks.Pickle.Web;

/// <summary>
/// Creates the driver and seeds the tree once defs load. Doing it in PickleMod's
/// constructor would capture a mod-loading thread as the main one.
/// </summary>
[StaticConstructorOnStartup]
public static class DashboardSeed {
  static DashboardSeed() {
    if (!PickleHttpServer.IsRunning) {
      return;
    }

    PickleDriver.EnsureExists();

    if (!GenCommandLine.CommandLineArgPassed("-pickle-run")) {
      RunnerWindow.Instance.PublishSnapshot();
    }
  }
}
