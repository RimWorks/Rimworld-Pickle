using System;

namespace RimWorks.Pickle.Core.Run;

/// <summary>A read of <see cref="TickCostSampler"/> over some number of recent ticks.</summary>
public readonly struct TickCostWindow : IEquatable<TickCostWindow> {
  /// <summary>Initializes a new instance.</summary>
  /// <param name="count">How many samples the window actually covers.</param>
  /// <param name="meanMs">The mean cost across the window, in milliseconds.</param>
  /// <param name="maxMs">The slowest tick in the window, in milliseconds.</param>
  /// <param name="maxIndexFromEnd">Where the slowest tick sat, counting back from the newest.</param>
  public TickCostWindow(int count, double meanMs, double maxMs, int maxIndexFromEnd) {
    Count = count;
    MeanMs = meanMs;
    MaxMs = maxMs;
    MaxIndexFromEnd = maxIndexFromEnd;
  }

  /// <summary>How many samples this window actually covers, not how many were asked for.</summary>
  public int Count { get; }

  /// <summary>The mean cost across the window, in milliseconds.</summary>
  public double MeanMs { get; }

  /// <summary>The slowest tick in the window, in milliseconds.</summary>
  public double MaxMs { get; }

  /// <summary>Where the slowest tick sat, counting back from the newest, which is zero.</summary>
  public int MaxIndexFromEnd { get; }

  /// <summary>Compares two windows field by field.</summary>
  /// <param name="left">The first window.</param>
  /// <param name="right">The second window.</param>
  /// <returns><c>true</c> when every field matches.</returns>
  public static bool operator ==(TickCostWindow left, TickCostWindow right) => left.Equals(right);

  /// <summary>Compares two windows field by field.</summary>
  /// <param name="left">The first window.</param>
  /// <param name="right">The second window.</param>
  /// <returns><c>true</c> when any field differs.</returns>
  public static bool operator !=(TickCostWindow left, TickCostWindow right) => !left.Equals(right);

  /// <summary>Compares this window to another field by field.</summary>
  /// <param name="other">The window to compare against.</param>
  /// <returns><c>true</c> when every field matches.</returns>
  public bool Equals(TickCostWindow other) {
    return Count == other.Count && MeanMs.Equals(other.MeanMs)
        && MaxMs.Equals(other.MaxMs) && MaxIndexFromEnd == other.MaxIndexFromEnd;
  }

  /// <summary>Compares this window to another object.</summary>
  /// <param name="obj">The object to compare against.</param>
  /// <returns><c>true</c> when <paramref name="obj"/> is a <see cref="TickCostWindow"/> with matching fields.</returns>
  public override bool Equals(object? obj) => obj is TickCostWindow other && Equals(other);

  /// <summary>Combines the window's fields into one hash code.</summary>
  /// <returns>The hash code.</returns>
  public override int GetHashCode() {
    return (Count, MeanMs, MaxMs, MaxIndexFromEnd).GetHashCode();
  }
}
