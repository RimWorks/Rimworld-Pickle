using System.Collections.Generic;
using Verse;

namespace Pickle;

public static class LogWatch {
  private static readonly object Gate = new object();
  private static readonly CircularBuffer<string> ErrorBuffer = new CircularBuffer<string>(50);
  private static bool armed;

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
