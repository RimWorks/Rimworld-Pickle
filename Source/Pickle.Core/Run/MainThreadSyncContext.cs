using System;
using System.Runtime.CompilerServices;
using System.Threading;

namespace RimWorks.Pickle.Core.Run;

/// <summary>
/// Routes every await continuation through the driver's main thread queue, so step code always
/// resumes on the thread Verse allows. Without one, .NET may queue a continuation to the pool.
/// </summary>
public sealed class MainThreadSyncContext : SynchronizationContext {
  private readonly Action<Action> postToMainThread;
  private readonly Func<bool> onMainThread;

  /// <summary>Install the result on the main thread itself; a context is per thread.</summary>
  /// <param name="postToMainThread">Queues an action to run on the main thread.</param>
  /// <param name="onMainThread">Whether the calling thread is the main thread.</param>
  public MainThreadSyncContext(Action<Action> postToMainThread, Func<bool> onMainThread) {
    this.postToMainThread = postToMainThread ?? throw new ArgumentNullException(nameof(postToMainThread));
    this.onMainThread = onMainThread ?? throw new ArgumentNullException(nameof(onMainThread));
  }

  /// <summary>Queues the callback for the main thread and returns without waiting.</summary>
  /// <param name="d">The callback to run.</param>
  /// <param name="state">The state to pass it.</param>
  public override void Post(SendOrPostCallback d, object? state) {
    if (d == null) {
      throw new ArgumentNullException(nameof(d));
    }

    postToMainThread(() => d(state));
  }

  /// <summary>
  /// Runs the callback on the main thread and blocks until it finishes. Called from the main
  /// thread it runs inline, since queueing would wait on a drain the same call is blocking.
  /// </summary>
  /// <param name="d">The callback to run.</param>
  /// <param name="state">The state to pass it.</param>
  public override void Send(SendOrPostCallback d, object? state) {
    if (d == null) {
      throw new ArgumentNullException(nameof(d));
    }

    if (onMainThread()) {
      d(state);
      return;
    }

    using ManualResetEventSlim done = new ManualResetEventSlim(false);

    // a field, not a captured local: sonar's symbolic execution does not follow a write
    // made inside the lambda and reads the null check below as dead
    StrongBox<Exception?> fault = new StrongBox<Exception?>(null);

    postToMainThread(() => {
      try {
        d(state);
      } catch (Exception ex) {
        fault.Value = ex;
      } finally {
        done.Set();
      }
    });

    done.Wait();

    if (fault.Value != null) {
      throw new InvalidOperationException("a Send callback threw on the main thread", fault.Value);
    }
  }

  /// <summary>Returns this context, which holds no per-copy state.</summary>
  /// <returns>This instance.</returns>
  public override SynchronizationContext CreateCopy() {
    return this;
  }
}
