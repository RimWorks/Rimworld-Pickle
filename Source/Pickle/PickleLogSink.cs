using RimWorks.RimLogging;

namespace RimWorks.Pickle;

/// <summary>The channel every Pickle log line goes to, so a player can filter the game log by mod.</summary>
public static class PickleLog {
  /// <summary>The channel name passed to each RimLogging call.</summary>
  public const string Channel = "Pickle";
}

/// <summary>
/// Feeds LogWatch from RimLogging's pipeline instead of a patch on Verse.Log.Error, so an
/// error reported through Verse.Log, UnityEngine.Debug or RimLogging all land here.
/// </summary>
public sealed class PickleLogSink : ILogSink {
  /// <inheritdoc/>
  public string Name => "pickle-logwatch";

  /// <inheritdoc/>
  public LogLevel MinLevel => LogLevel.Warn;

  /// <inheritdoc/>
  // Checked here as well as declared above: the registry dispatches every entry to every
  // sink, so a sink that trusts MinLevel records Info lines as errors.
  public void Write(LogEntry entry) {
    if (entry.Level < MinLevel) {
      return;
    }

    if (entry.Level == LogLevel.Warn) {
      LogWatch.RecordWarning(entry.RenderedMessage, entry.Mod);
    } else {
      LogWatch.RecordError(entry.RenderedMessage);
    }
  }

  /// <inheritdoc/>
  public void Flush() {
  }

  /// <inheritdoc/>
  public void Dispose() {
  }
}
