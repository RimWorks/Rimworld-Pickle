using System;
using System.Collections.Generic;
using Verse;

namespace RimWorks.Pickle;

/// <summary>Watches for logged errors while a scenario runs, so the runner can fail one that
/// caused no assert to fail but still logged an error.</summary>
public static class LogWatch {
  private static readonly object Gate = new object();
  private static readonly CircularBuffer<string> ErrorBuffer = new CircularBuffer<string>(50);

  // Its own buffer, not a filter over ErrorBuffer: a warning must never be able to trip the
  // error gate in RunSession, so it cannot share storage with what that gate reads.
  private static readonly CircularBuffer<(string Message, string? Mod)> WarningBuffer =
      new CircularBuffer<(string Message, string? Mod)>(50);

  // Engine noise no mod can prevent, so a scenario must not fail on it. Wine answers the
  // multi-monitor call with a failure whose own text reads "Success".
  private static readonly string[] IgnoredErrors = ["MonitorFromWindow failed"];
  private static bool armed;
  private static long totalRecorded;
  private static long totalWarningsRecorded;
  private static long warningMarkAtArm;
  private static int outsideScenario;
  private static bool everArmed;

  /// <summary>How many errors landed while no scenario was running, so none could be blamed for
  /// them. A non-zero count means something threw outside every scenario's window.</summary>
  public static int OutsideScenarioCount {
    get {
      lock (Gate) {
        return outsideScenario;
      }
    }
  }

  /// <summary>Whether a scenario is currently watching for errors.</summary>
  public static bool Armed {
    get {
      lock (Gate) {
        return armed;
      }
    }
  }

  /// <summary>Every error recorded since the last <see cref="Arm"/>, oldest first.</summary>
  public static IReadOnlyList<string> ErrorsSinceArmed {
    get {
      lock (Gate) {
        return ErrorBuffer.GetSnapshot();
      }
    }
  }

  /// <summary>How many errors are currently held since the last <see cref="Arm"/>.</summary>
  public static int ErrorCount {
    get {
      lock (Gate) {
        return ErrorBuffer.Count;
      }
    }
  }

  /// <summary>Every warning recorded since the last <see cref="Arm"/>, oldest first, paired
  /// with the attributed mod, or <see langword="null"/> when it could not attribute one.</summary>
  public static IReadOnlyList<(string Message, string? Mod)> WarningsSinceArmed {
    get {
      lock (Gate) {
        return WarningBuffer.GetSnapshot();
      }
    }
  }

  /// <summary>How many warnings since <see cref="Arm"/> have rolled out of the 50-slot buffer.
  /// Non-zero means a "no warning" step cannot tell an empty buffer from a wiped one.</summary>
  public static long WarningsDroppedSinceArmed {
    get {
      lock (Gate) {
        long recorded = totalWarningsRecorded - warningMarkAtArm;
        return Math.Max(0, recorded - WarningBuffer.Count);
      }
    }
  }

  /// <summary>
  /// Errors recorded since the process started. Take one of these before an action and
  /// pass it to <see cref="ErrorsSince"/> to see only what that action logged.
  /// </summary>
  public static long Mark {
    get {
      lock (Gate) {
        return totalRecorded;
      }
    }
  }

  /// <summary>Errors recorded after a given <see cref="Mark"/>. A burst bigger than the 50-entry
  /// buffer reports only its tail.</summary>
  /// <param name="mark">A value previously read from <see cref="Mark"/>.</param>
  /// <returns>The errors logged since <paramref name="mark"/>, oldest first.</returns>
  public static IReadOnlyList<string> ErrorsSince(long mark) {
    lock (Gate) {
      long since = totalRecorded - mark;
      if (since <= 0) {
        return [];
      }

      // The buffer holds 50, so a burst larger than that reports only its tail.
      List<string> snapshot = ErrorBuffer.GetSnapshot();
      int take = (int)Math.Min(since, snapshot.Count);
      return snapshot.GetRange(snapshot.Count - take, take);
    }
  }

  /// <summary>Clears both buffers and starts recording, for a scenario about to run.</summary>
  public static void Arm() {
    lock (Gate) {
      ErrorBuffer.Clear();
      WarningBuffer.Clear();
      warningMarkAtArm = totalWarningsRecorded;
      armed = true;
      everArmed = true;
    }
  }

  /// <summary>Clears the error buffer only, for a fixture load whose vanilla noise should not
  /// fail the scenario that asked for it.</summary>
  public static void ArmAfterLoad() {
    lock (Gate) {
      // the load is where the warnings worth asserting on come from, so keep them. only the
      // error gate needs protecting from load noise.
      ErrorBuffer.Clear();
      armed = true;
      everArmed = true;
    }
  }

  /// <summary>Stops recording errors, for a scenario that has finished.</summary>
  public static void Disarm() {
    lock (Gate) {
      armed = false;
    }
  }

  /// <summary>Records a logged error, unless nothing is armed or the message is known engine
  /// noise. Called from the log sink for every error the game logs.</summary>
  /// <param name="message">The rendered log message.</param>
  public static void RecordError(string message) {
    lock (Gate) {
      if (IsIgnored(message)) {
        return;
      }

      // Recording never stops. An async callback can land a frame after its scenario ends,
      // and dropping it there made a real error invisible. Arm clears the buffer, so the
      // next scenario still starts clean and is never blamed for one that arrived late.
      ErrorBuffer.Enqueue(message);
      totalRecorded++;

      // Only after the first scenario has armed. Boot errors are not "between scenarios",
      // and counting them would leave this non-zero on every run and mean nothing.
      if (!armed && everArmed) {
        outsideScenario++;
      }
    }
  }

  /// <summary>Records a logged warning. Never gates a scenario the way <see cref="RecordError"/>
  /// does, so it carries no ignore list and no outside-scenario count.</summary>
  /// <param name="message">The rendered log message.</param>
  /// <param name="mod">The mod RimLogging attributed the entry to, or <see langword="null"/>.</param>
  public static void RecordWarning(string message, string? mod) {
    lock (Gate) {
      WarningBuffer.Enqueue((message, mod));
      totalWarningsRecorded++;
    }
  }

  private static bool IsIgnored(string message) {
    foreach (string ignored in IgnoredErrors) {
      if (message.IndexOf(ignored, StringComparison.Ordinal) >= 0) {
        return true;
      }
    }

    return false;
  }

  private sealed class CircularBuffer<T> {
    private readonly T[] buffer;
    private int head;

    public CircularBuffer(int capacity) {
      buffer = new T[capacity];
      head = 0;
      Count = 0;
    }

    public int Count { get; private set; }

    public void Enqueue(T item) {
      buffer[head] = item;
      head = (head + 1) % buffer.Length;
      if (Count < buffer.Length) {
        Count++;
      }
    }

    public void Clear() {
      head = 0;
      Count = 0;
    }

    public List<T> GetSnapshot() {
      List<T> snapshot = [];
      for (int i = 0; i < Count; i++) {
        int index = (head - Count + i + buffer.Length) % buffer.Length;
        snapshot.Add(buffer[index]!);
      }

      return snapshot;
    }
  }
}
