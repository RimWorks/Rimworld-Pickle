using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace Pickle.Runtime;

/// <summary>
/// Awaitable returned by driver waits. PickleDriver.Update invokes the continuation
/// directly, which is what guarantees await resumes on Unity's main thread.
/// </summary>
public readonly struct PickleWait : INotifyCompletion {
  private readonly PendingWait? pendingWait;
  private readonly Exception? immediateFault;

  internal PickleWait(PendingWait pendingWait) {
    this.pendingWait = pendingWait;
    immediateFault = null;
  }

  internal PickleWait(Exception immediateFault) {
    pendingWait = null;
    this.immediateFault = immediateFault;
  }

  public bool IsCompleted => immediateFault != null || (pendingWait != null && pendingWait.IsResolved);

  public PickleWait GetAwaiter() {
    return this;
  }

  /// <inheritdoc/>
  public void OnCompleted(Action continuation) {
    if (immediateFault != null) {
      continuation();
      return;
    }

    // TryAttach fails when the driver resolved the wait between IsCompleted and here,
    // in which case nobody would ever invoke the continuation - so run it now.
    if (!pendingWait!.TryAttach(continuation)) {
      continuation();
    }
  }

  public void GetResult() {
    Exception? fault = immediateFault ?? pendingWait?.Fault;
    if (fault != null) {
      ExceptionDispatchInfo.Capture(fault).Throw();
    }
  }
}
