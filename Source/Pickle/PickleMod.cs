using System.Collections.Generic;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Patching;
using RimWorks.Pickle.Web;
using RimWorks.RimLogging;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle;

/// <summary>RimWorld's entry point into Pickle. Its constructor wires up logging, applies patches,
/// discovers suites, and starts the dashboard server.</summary>
public class PickleMod : Mod {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="content">The mod content pack RimWorld constructs this from.</param>
  public PickleMod(ModContentPack content) : base(content) {
    // Before anything else logs: LogWatch is fed from this sink now, so an error raised
    // during startup is only recorded once the sink is registered.
    Logging.RegisterSink(new PickleLogSink());

    Log.InfoTo(PickleLog.Channel, "loaded");

    // Last point before RimWorld applies XML patches, which is the only chance to see
    // which mod patches which def.
    PatchBackends.ApplyEarliest();

    List<DiscoveredSuite> suites = SuiteScanner.DiscoverSuites();
    SuiteScanner.LogSuites(suites);

    PickleHttpServer.StartUnlessDisabled();
  }
}
