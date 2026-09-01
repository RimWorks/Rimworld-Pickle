using RimWorks.RimLogging;

namespace RimWorks.Pickle;

/// <summary>
/// Feeds LogWatch from RimLogging's pipeline instead of a patch on Verse.Log.Error.
/// RimLogging already captures Verse.Log and UnityEngine.Debug, so this sees an error
/// whichever of the three a mod used to report it.
/// </summary>
internal sealed class PickleLogSink : ILogSink {
  public string Name => "pickle-logwatch";

  public LogLevel MinLevel => LogLevel.Error;

  public void Write(LogEntry entry) {
    LogWatch.RecordError(entry.RenderedMessage);
  }

  public void Flush() {
  }

  public void Dispose() {
  }
}
