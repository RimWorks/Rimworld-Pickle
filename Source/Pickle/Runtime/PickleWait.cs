using System;
using System.Runtime.CompilerServices;
using System.Runtime.ExceptionServices;

namespace RimWorks.Pickle.Runtime;

/// <summary>
/// Awaitable returned by driver waits. PickleDriver.Update invokes the continuation
/// directly, which is what guarantees await resumes on Unity's main thread.
/// </summary>
public readonly struct PickleWait : INotifyCompletion {
  private readonly PendingWait? pendingWait;
  private readonly Exception? immediateFault;

  /// <summary>Initializes a new instance.</summary>
  /// <param name="pendingWait">The driver's registered wait to resolve against.</param>
  internal PickleWait(PendingWait pendingWait) {
    this.pendingWait = pendingWait;
    immediateFault = null;
  }

  /// <summary>Initializes a new instance that is already faulted, with nothing to wait on.</summary>
  /// <param name="immediateFault">The exception to throw as soon as this is awaited.</param>
  internal PickleWait(Exception immediateFault) {
    pendingWait = null;
    this.immediateFault = immediateFault;
  }

  /// <summary>Whether the wait already resolved, so <c>await</c> can skip scheduling a continuation.</summary>
  public bool IsCompleted => immediateFault != null || (pendingWait != null && pendingWait.IsResolved);

  /// <summary>Returns itself; a <see cref="PickleWait"/> is its own awaiter.</summary>
  /// <returns>This instance.</returns>
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

  /// <summary>Rethrows the wait's fault, if any, so <c>await</c> surfaces it at the call site.</summary>
  public void GetResult() {
    Exception? fault = immediateFault ?? pendingWait?.Fault;
    if (fault != null) {
      ExceptionDispatchInfo.Capture(fault).Throw();
    }
  }
}
