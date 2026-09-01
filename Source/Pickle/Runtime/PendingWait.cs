using System;

namespace RimWorks.Pickle.Runtime;

internal enum PendingWaitKind {
  Ticks,
  Frames,
  Until,
}

internal sealed class PendingWait {
  // Waits register from background threads but resolve on the main one. Without this
  // gate a continuation attached just after resolution would hang forever.
  private readonly object gate = new object();

  private Action? continuation;

  public PendingWait(PendingWaitKind kind) {
    Kind = kind;
  }

  public PendingWaitKind Kind { get; }

  public object? Scope { get; set; }

  public int TargetTicksGame { get; set; }

  public int TargetFrame { get; set; }

  public Func<bool>? Condition { get; set; }

  public float DeadlineRealtime { get; set; }

  public float TimeoutSeconds { get; set; }

  public bool IsResolved { get; private set; }

  public Exception? Fault { get; private set; }

  public bool TryAttach(Action attaching) {
    lock (gate) {
      if (IsResolved) {
        return false;
      }

      continuation = attaching;
      return true;
    }
  }

  public Action? Resolve(Exception? fault) {
    lock (gate) {
      if (IsResolved) {
        return null;
      }

      IsResolved = true;
      Fault = fault;

      Action? resolved = continuation;
      continuation = null;
      return resolved;
    }
  }
}
