using RimWorks.RimLogging;

namespace RimWorks.Pickle;

/// <summary>
/// Feeds LogWatch from RimLogging's pipeline instead of a patch on Verse.Log.Error, so an
/// error reported through Verse.Log, UnityEngine.Debug or RimLogging all land here.
/// </summary>
internal sealed class PickleLogSink : ILogSink {
  public string Name => "pickle-logwatch";

  public LogLevel MinLevel => LogLevel.Error;

  // Checked here as well as declared above: the registry dispatches every entry to every
  // sink, so a sink that trusts MinLevel records Info lines as errors.
  public void Write(LogEntry entry) {
    if (entry.Level < MinLevel) {
      return;
    }

    LogWatch.RecordError(entry.RenderedMessage);
  }

  public void Flush() {
  }

  public void Dispose() {
  }
}
