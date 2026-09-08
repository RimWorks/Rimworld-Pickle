using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using RimWorks.Pickle.Input;
using RimWorks.Pickle.Runtime;
using UnityEngine;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle;

/// <summary>State scoped to one scenario: asserts, attachments, the value bag steps share, and
/// the input and wait helpers that drive the game. A new instance per scenario.</summary>
public class PickleContext {
  private readonly List<AssertRecord> asserts = [];
  private readonly List<(string Name, string Content)> attachments = [];
  private readonly Dictionary<Type, object> bag = new();

  /// <summary>Every assert made in this scenario so far, passed or failed.</summary>
  public IReadOnlyList<AssertRecord> Asserts => asserts;

  /// <summary>Every named blob attached to this scenario's report so far.</summary>
  public IReadOnlyList<(string Name, string Content)> Attachments => attachments;

  /// <summary>The seed for this scenario's random draws, from the run seed or an <c>@seed:</c> tag.</summary>
  // Rand is one global stream the running game also draws from, so a step that needs a
  // reproducible draw has to reseed right before it rather than trust the scenario seed.
  public int ScenarioSeed { get; internal set; }

  /// <summary>The current step's wait scope, so a fault raised against it lands on the step that is
  /// actually waiting rather than a stale one.</summary>
  internal object? WaitScope { get; set; }

  /// <summary>Records and checks a condition, throwing when it does not hold.</summary>
  /// <param name="condition">The condition to check.</param>
  /// <param name="label">The failure message, shown only when <paramref name="condition"/> is <c>false</c>.</param>
  public void Assert(bool condition, string? label = null) {
    asserts.Add(new AssertRecord(condition, label));
    if (condition) {
      return;
    }

    throw new PickleAssertionException(label ?? "Assertion failed.");
  }

  /// <summary>Waits for a condition before asserting it, so state the game assigns on a later
  /// think cycle gets a chance to land first.</summary>
  /// <param name="condition">The condition to wait for and then assert.</param>
  /// <param name="describeFailure">Builds the failure message, called only if the assert fails.</param>
  /// <param name="timeoutSeconds">How long to wait before asserting anyway.</param>
  /// <returns>A task that completes once the condition holds, faulted with the described failure when it never does.</returns>
  // RimWorld assigns state on the next think cycle, so asserting straight after an
  // action races the game. The message is built at failure to show the final state.
  public async Task AssertEventually(Func<bool> condition, Func<string> describeFailure, float timeoutSeconds = 2f) {
    if (!condition()) {
      try {
        await WaitUntil(condition, timeoutSeconds);
      } catch (TimeoutException) {
        // swallowed so Assert below reports the real state, not "wait timed out"
      }
    }

    Assert(condition(), describeFailure());
  }

  /// <summary>Checks a setup precondition, throwing <see cref="PickleRequireException"/> when it
  /// fails so the scenario reports a broken setup rather than a failed expectation.</summary>
  /// <param name="condition">The precondition to check.</param>
  /// <param name="hint">What was required and was not there.</param>
  public void Require(bool condition, string hint) {
    if (condition) {
      return;
    }

    throw new PickleRequireException(hint);
  }

  /// <summary>Waits for the game to advance a number of ticks.</summary>
  /// <param name="n">The number of ticks to wait for.</param>
  /// <returns>An awaitable that resolves once the ticks have passed.</returns>
  public PickleWait WaitTicks(int n) {
    return PickleDriver.Instance.WaitTicks(n, WaitScope);
  }

  /// <summary>Waits for a number of rendered frames.</summary>
  /// <param name="n">The number of frames to wait for.</param>
  /// <returns>An awaitable that resolves once the frames have passed.</returns>
  public PickleWait WaitFrames(int n) {
    return PickleDriver.Instance.WaitFrames(n, WaitScope);
  }

  /// <summary>Waits for a condition to become true, or times out.</summary>
  /// <param name="condition">The condition to poll.</param>
  /// <param name="timeoutSeconds">How long to wait before the awaitable throws <see cref="TimeoutException"/>.</param>
  /// <returns>An awaitable that resolves once <paramref name="condition"/> is <c>true</c>.</returns>
  public PickleWait WaitUntil(Func<bool> condition, float timeoutSeconds = 5f) {
    return PickleDriver.Instance.WaitUntil(condition, timeoutSeconds, WaitScope);
  }

  /// <summary>Stores a value in this scenario's bag, keyed by its type. A second call with the
  /// same type replaces the first.</summary>
  /// <typeparam name="T">The type to key the value by.</typeparam>
  /// <param name="value">The value to store.</param>
  public void Set<T>(T value) {
    bag[typeof(T)] = value!;
  }

  /// <summary>Reads back a value a previous step stored with <see cref="Set{T}"/>. Throws if none
  /// was set for <typeparamref name="T"/>.</summary>
  /// <typeparam name="T">The type the value was stored under.</typeparam>
  /// <returns>The stored value.</returns>
  public T Get<T>() {
    if (bag.TryGetValue(typeof(T), out object? value)) {
      return (T)value;
    }

    throw new InvalidOperationException(
        $"PickleContext.Get<{typeof(T).Name}>: no value of type {typeof(T).Name} has been set.");
  }

  /// <summary>Waits for a tagged rect to appear, moves the pointer onto it, and clicks. Throws if
  /// the tag never resolves.</summary>
  /// <param name="tag">The tag a mod's <c>OnGUI</c> recorded with <see cref="PickleUI.Tag"/>.</param>
  /// <returns>A task that completes once the tag resolves and the click has been dispatched.</returns>
  public async Task Click(string tag) {
    try {
      await WaitUntil(() => TagInteractor.TryResolve(tag, out _, out _), 5f);
    } catch (TimeoutException) {
      throw new InvalidOperationException(TagInteractor.DescribeMiss(tag));
    }

    if (!TagInteractor.TryResolve(tag, out Rect rect, out string? error)) {
      throw new InvalidOperationException(error ?? "Failed to resolve tag");
    }

    await MovePointerTo(rect.center);
    InputBackends.Current.Click(rect.center);
    await WaitFrames(2);
  }

  /// <summary>Waits for a tagged rect to appear and moves the pointer onto it, without clicking.
  /// Throws if the tag never resolves.</summary>
  /// <param name="tag">The tag a mod's <c>OnGUI</c> recorded with <see cref="PickleUI.Tag"/>.</param>
  /// <returns>A task that completes once the tag resolves and the pointer has been moved onto it.</returns>
  public async Task Hover(string tag) {
    try {
      await WaitUntil(() => TagInteractor.TryResolve(tag, out _, out _), 5f);
    } catch (TimeoutException) {
      throw new InvalidOperationException(TagInteractor.DescribeMiss(tag));
    }

    if (!TagInteractor.TryResolve(tag, out Rect rect, out string? error)) {
      throw new InvalidOperationException(error ?? "Failed to resolve tag");
    }

    await MovePointerTo(rect.center);
    await WaitFrames(1);
  }

  /// <summary>Sends a key press through the active input backend.</summary>
  /// <param name="key">The key to press.</param>
  /// <returns>A task that completes two frames after the key is sent, so the game has processed it.</returns>
  public async Task PressKey(string key) {
    InputBackends.EnsureAvailable();
    InputBackends.Current.Key(key);
    await WaitFrames(2);
  }

  /// <summary>Attaches a named blob of content to this scenario's report.</summary>
  /// <param name="name">The name shown for this attachment in the report.</param>
  /// <param name="content">The attachment's content.</param>
  public void Attach(string name, string content) {
    attachments.Add((name, content));
  }

  // An unfocused window is the one state where the OS moves the cursor and the game never
  // reads it, so focus is recorded beside both readings rather than guessed from them.
  private static string DescribePointer(string lead, Vector2 guiPoint) {
    return $"{lead} {guiPoint}: the OS reports {InputBackends.Current.GetMouseLocation()}, "
        + $"the game reads {Verse.UI.MousePositionOnUIInverted}, focused={Application.isFocused}";
  }

  // The OS cursor moving is not the same as the game seeing it move: a click sent before
  // Unity has read the new position lands with the pointer still where it was and
  // activates nothing, which is silent. Three pixels covers the rounding SendInput's
  // 0-65535 grid costs.
  private async Task MovePointerTo(Vector2 guiPoint) {
    InputBackends.EnsureAvailable();
    InputBackends.Current.MoveTo(guiPoint);

    try {
      await WaitUntil(() => (Verse.UI.MousePositionOnUIInverted - guiPoint).sqrMagnitude <= 9f, 2f);
    } catch (TimeoutException) {
      throw new InvalidOperationException(DescribePointer("the pointer never reached", guiPoint));
    }

    Log.InfoTo("Pickle", $"{DescribePointer("pointer at", guiPoint)}");
  }
}
