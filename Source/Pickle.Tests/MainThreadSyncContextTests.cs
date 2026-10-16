using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Run;
using Xunit;

namespace RimWorks.Pickle.Tests;

public class MainThreadSyncContextTests {
  private readonly ConcurrentQueue<Action> queue = new();
  private readonly int mainThreadId = Thread.CurrentThread.ManagedThreadId;
  private readonly MainThreadSyncContext context;

  public MainThreadSyncContextTests() {
    context = new MainThreadSyncContext(
        queue.Enqueue,
        () => Thread.CurrentThread.ManagedThreadId == mainThreadId);
  }

  [Fact]
  public void Post_QueuesInsteadOfRunning() {
    bool ran = false;
    context.Post(_ => ran = true, null);

    Assert.False(ran);
    Assert.Single(queue);

    Drain();
    Assert.True(ran);
  }

  [Fact]
  public void Send_RunsInlineOnTheMainThread() {
    bool ran = false;
    context.Send(_ => ran = true, null);

    Assert.True(ran);
    Assert.Empty(queue);
  }

  [Fact]
  public void Send_FromAnotherThread_RunsOnTheMainThreadAndBlocks() {
    int ranOn = 0;
    Task sender = Task.Run(() => context.Send(_ => ranOn = Thread.CurrentThread.ManagedThreadId, null));

    PumpUntil(() => sender.IsCompleted);

    Assert.Null(sender.Exception);
    Assert.Equal(mainThreadId, ranOn);
  }

  [Fact]
  public void ChainedAwait_ResumesOnTheMainThreadNotThePool() {
    SynchronizationContext? restore = SynchronizationContext.Current;
    SynchronizationContext.SetSynchronizationContext(context);

    try {
      int resumedOn = 0;
      Task scenario = RunScenario(() => resumedOn = Thread.CurrentThread.ManagedThreadId);

      PumpUntil(() => scenario.IsCompleted);

      Assert.Null(scenario.Exception);
      Assert.Equal(mainThreadId, resumedOn);
    } finally {
      SynchronizationContext.SetSynchronizationContext(restore);
    }
  }

  private static async Task RunScenario(Action afterLoad) {
    await LoadFixture();
    afterLoad();
  }

  private static async Task LoadFixture() {
    await Task.Run(() => Thread.Sleep(10));
  }

  private void PumpUntil(Func<bool> done) {
    DateTime deadline = DateTime.UtcNow.AddSeconds(5);
    while (!done()) {
      Assert.True(DateTime.UtcNow < deadline, "nothing reached the main thread queue within 5s");
      Drain();
      Thread.Sleep(1);
    }

    Drain();
  }

  private void Drain() {
    while (queue.TryDequeue(out Action? action)) {
      action();
    }
  }
}
