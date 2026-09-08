namespace RimWorks.Pickle.UI;

/// <summary>What the user chose on a <see cref="BreakCard"/> shown for a failed step.</summary>
public enum BreakCardDecision {
  /// <summary>Resume the run past the failure.</summary>
  Continue,

  /// <summary>Stop the run.</summary>
  Abort,
}
