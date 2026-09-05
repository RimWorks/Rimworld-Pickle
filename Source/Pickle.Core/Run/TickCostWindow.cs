using System;

namespace RimWorks.Pickle.Core.Run;

/// <summary>A read of <see cref="TickCostSampler"/> over some number of recent ticks.</summary>
public readonly struct TickCostWindow : IEquatable<TickCostWindow> {
  public TickCostWindow(int count, double meanMs, double maxMs, int maxIndexFromEnd) {
    Count = count;
    MeanMs = meanMs;
    MaxMs = maxMs;
    MaxIndexFromEnd = maxIndexFromEnd;
  }

  /// <summary>How many samples this window actually covers, not how many were asked for.</summary>
  public int Count { get; }

  public double MeanMs { get; }

  public double MaxMs { get; }

  /// <summary>Where the slowest tick sat, counting back from the newest, which is zero.</summary>
  public int MaxIndexFromEnd { get; }

  public static bool operator ==(TickCostWindow left, TickCostWindow right) => left.Equals(right);

  public static bool operator !=(TickCostWindow left, TickCostWindow right) => !left.Equals(right);

  public bool Equals(TickCostWindow other) {
    return Count == other.Count && MeanMs.Equals(other.MeanMs)
        && MaxMs.Equals(other.MaxMs) && MaxIndexFromEnd == other.MaxIndexFromEnd;
  }

  public override bool Equals(object? obj) => obj is TickCostWindow other && Equals(other);

  public override int GetHashCode() {
    return (Count, MeanMs, MaxMs, MaxIndexFromEnd).GetHashCode();
  }
}
