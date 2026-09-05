using System;
using System.Diagnostics;

namespace RimWorks.Pickle.Core.Run;

/// <summary>
/// What each game tick cost, in a fixed ring so a long scenario cannot grow it. Fed one
/// sample per tick from the fast-mode wait step, which is the only place Pickle drives
/// the tick loop itself.
/// </summary>
public static class TickCostSampler {
  public const int Capacity = 10_000;

  private static readonly long[] Samples = new long[Capacity];

  private static int next;

  /// <summary>How many samples have been recorded, which can exceed <see cref="Capacity"/>.</summary>
  public static int Count { get; private set; }

  /// <summary>Takes a raw Stopwatch delta. Converted to milliseconds only on read.</summary>
  public static void Record(long stopwatchTicks) {
    Samples[next] = stopwatchTicks;
    next = (next + 1) % Capacity;
    Count++;
  }

  public static void Reset() {
    next = 0;
    Count = 0;
  }

  /// <summary>
  /// The most recent n samples. Fewer than asked for is reported rather than padded, so a
  /// budget that never had the ticks it wanted can fail instead of passing on three.
  /// </summary>
  public static TickCostWindow Window(int n) {
    int available = Math.Min(Count, Capacity);
    int take = Math.Min(n, available);
    if (take <= 0) {
      return default;
    }

    double total = 0;
    long max = long.MinValue;
    int maxFromEnd = 0;

    for (int i = 0; i < take; i++) {
      long sample = Samples[(((next - 1 - i) % Capacity) + Capacity) % Capacity];
      total += sample;
      if (sample > max) {
        max = sample;
        maxFromEnd = i;
      }
    }

    return new TickCostWindow(take, ToMs(total / take), ToMs(max), maxFromEnd);
  }

  private static double ToMs(double stopwatchTicks) {
    return stopwatchTicks * 1000.0 / Stopwatch.Frequency;
  }
}
