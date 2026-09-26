using System;

namespace RimWorks.Pickle.Core.Ui;

/// <summary>
/// Counts how many frames in a row a value has been the same. A step feeds it one observation per
/// frame and asks whether the value has stood still long enough. It holds no RimWorld type, so the
/// counting stays unit testable; the caller supplies the rectangle.
/// </summary>
/// <typeparam name="T">The observed value, compared with <see cref="IEquatable{T}.Equals(T)"/>.</typeparam>
public sealed class StillnessTracker<T>
    where T : struct, IEquatable<T> {
  private readonly int framesRequired;

  /// <summary>Initializes a new instance.</summary>
  /// <param name="framesRequired">How many frames in a row the value must be identical.</param>
  public StillnessTracker(int framesRequired) {
    if (framesRequired < 1) {
      throw new ArgumentOutOfRangeException(nameof(framesRequired), framesRequired, "at least one frame is required");
    }

    this.framesRequired = framesRequired;
  }

  /// <summary>How many frames in a row, up to and including the latest, the value was identical.</summary>
  public int StillFrames { get; private set; }

  /// <summary>Whether the value has been identical for the required number of frames.</summary>
  public bool HasStoodStill => StillFrames >= framesRequired;

  /// <summary>Whether any observation has held a value.</summary>
  public bool EverSeen => LastSeen.HasValue;

  /// <summary>The latest value observed, kept when a later frame observes nothing.</summary>
  public T? LastSeen { get; private set; }

  /// <summary>Records one frame.</summary>
  /// <param name="value">What the frame held, or <c>null</c> when it held nothing.</param>
  // A frame that holds nothing is not a still frame: the control was gone or being redrawn, so
  // the run starts again the next time it appears.
  public void Observe(T? value) {
    if (!value.HasValue) {
      StillFrames = 0;
      return;
    }

    StillFrames = LastSeen.HasValue && StillFrames > 0 && LastSeen.Value.Equals(value.Value) ? StillFrames + 1 : 1;
    LastSeen = value;
  }
}
