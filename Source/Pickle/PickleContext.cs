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

  /// <summary>Waits for a tagged rect to appear and clicks it. Throws if the tag never resolves, or
  /// if nothing at that rect is clickable.</summary>
  /// <param name="tag">The tag a mod's <c>OnGUI</c> recorded with <see cref="PickleUI.Tag"/>.</param>
  /// <returns>A task that completes once the click has been taken by a widget.</returns>
  public async Task Click(string tag) {
    Rect rect = await ResolveTag(tag);

    InteractionRequest.ArmClick(rect);
    try {
      await WaitUntil(() => InteractionRequest.ClickFired, 2f);
    } catch (TimeoutException) {
      InteractionRequest.Clear();
      throw new InvalidOperationException(
          $"tag '{tag}' resolved to {rect} but no button there took the click; "
          + "the element has to route through Widgets.ButtonInvisible for a click step to reach it");
    }

    await WaitFrames(2);
  }

  /// <summary>Waits for a tagged rect to appear and holds the pointer over it, without clicking.
  /// Throws if the tag never resolves.</summary>
  /// <param name="tag">The tag a mod's <c>OnGUI</c> recorded with <see cref="PickleUI.Tag"/>.</param>
  /// <returns>A task that completes once the rect reports the pointer as over it.</returns>
  public async Task Hover(string tag) {
    Rect rect = await ResolveTag(tag);

    InteractionRequest.SetHover(rect);
    await WaitFrames(1);
  }

  /// <summary>Presses a key, delivering it into the game's own OnGUI pass.</summary>
  /// <param name="key">The key to press.</param>
  /// <returns>A task that completes two frames after the key is sent, so the game has processed it.</returns>
  public async Task PressKey(string key) {
    EventSynth.RequestKeyEvent(EventSynth.Mechanism.UIRootReinvoke, KeyNames.Parse(key));
    await WaitFrames(2);

    if (EventSynth.TryTakeFailure(out Exception? failure)) {
      throw new InvalidOperationException($"key '{key}' could not be delivered", failure);
    }
  }

  /// <summary>Attaches a named blob of content to this scenario's report.</summary>
  /// <param name="name">The name shown for this attachment in the report.</param>
  /// <param name="content">The attachment's content.</param>
  public void Attach(string name, string content) {
    attachments.Add((name, content));
  }

  private async Task<Rect> ResolveTag(string tag) {
    try {
      await WaitUntil(() => TagInteractor.TryResolve(tag, out _, out _), 5f);
    } catch (TimeoutException) {
      throw new InvalidOperationException(TagInteractor.DescribeMiss(tag));
    }

    if (!TagInteractor.TryResolve(tag, out Rect rect, out string? error)) {
      throw new InvalidOperationException(error ?? "Failed to resolve tag");
    }

    return rect;
  }
}
