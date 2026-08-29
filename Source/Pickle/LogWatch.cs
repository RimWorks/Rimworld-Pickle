using System;
using System.Collections.Generic;
using Verse;

namespace Pickle;

public static class LogWatch {
  private static readonly object Gate = new object();
  private static readonly CircularBuffer<string> ErrorBuffer = new CircularBuffer<string>(50);
  private static bool armed;
  private static long totalRecorded;

  public static bool Armed {
    get {
      lock (Gate) {
        return armed;
      }
    }
  }

  public static IReadOnlyList<string> ErrorsSinceArmed {
    get {
      lock (Gate) {
        return ErrorBuffer.GetSnapshot();
      }
    }
  }

  public static int ErrorCount {
    get {
      lock (Gate) {
        return ErrorBuffer.Count;
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

  public static void Arm() {
    lock (Gate) {
      ErrorBuffer.Clear();
      armed = true;
    }
  }

  public static void Disarm() {
    lock (Gate) {
      armed = false;
    }
  }

  public static void RecordError(string message) {
    lock (Gate) {
      if (armed) {
        ErrorBuffer.Enqueue(message);
        totalRecorded++;
      }
    }
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
