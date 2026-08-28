using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Pickle.Input;
using Pickle.Runtime;
using UnityEngine;

namespace Pickle;

public class PickleContext {
  private readonly List<AssertRecord> asserts = [];
  private readonly List<(string Name, string Content)> attachments = [];
  private readonly Dictionary<Type, object> bag = new();

  internal object? WaitScope { get; set; }

  public IReadOnlyList<AssertRecord> Asserts => asserts;

  public IReadOnlyList<(string Name, string Content)> Attachments => attachments;

  public void Assert(bool condition, string? label = null) {
    asserts.Add(new AssertRecord(condition, label));
    if (condition) {
      return;
    }

    throw new PickleAssertionException(label ?? "Assertion failed.");
  }

  // RimWorld assigns state on the next think cycle, so asserting straight after an
  // action races the game. The message is built at failure to show the final state.
  public async Task AssertEventually(Func<bool> condition, Func<string> describeFailure, float timeoutSeconds = 2f) {
    if (!condition()) {
      try {
        await WaitUntil(condition, timeoutSeconds);
      } catch (TimeoutException) {
      }
    }

    Assert(condition(), describeFailure());
  }

  public void Require(bool condition, string hint) {
    if (condition) {
      return;
    }

    throw new PickleRequireException(hint);
  }

  public PickleWait WaitTicks(int n) {
    return PickleDriver.Instance.WaitTicks(n, WaitScope);
  }

  public PickleWait WaitFrames(int n) {
    return PickleDriver.Instance.WaitFrames(n, WaitScope);
  }

  public PickleWait WaitUntil(Func<bool> condition, float timeoutSeconds = 5f) {
    return PickleDriver.Instance.WaitUntil(condition, timeoutSeconds, WaitScope);
  }

  public void Set<T>(T value) {
    bag[typeof(T)] = value!;
  }

  public T Get<T>() {
    if (bag.TryGetValue(typeof(T), out object? value)) {
      return (T)value;
    }

    throw new InvalidOperationException(
        $"PickleContext.Get<{typeof(T).Name}>: no value of type {typeof(T).Name} has been set.");
  }

  public async Task Click(string tag) {
    try {
      await WaitUntil(() => TagInteractor.TryResolve(tag, out _, out _), 5f);
    } catch (TimeoutException) {
      throw new InvalidOperationException(TagInteractor.DescribeMiss(tag));
    }

    if (!TagInteractor.TryResolve(tag, out Rect rect, out string? error)) {
      throw new InvalidOperationException(error ?? "Failed to resolve tag");
    }

    if (!XdoInput.Available) {
      throw new InvalidOperationException(
          "xdotool is not available; the docker image needs xdotool installed for click injection.");
    }

    XdoInput.Click(rect.center);
    await WaitFrames(2);
  }

  public async Task Hover(string tag) {
    try {
      await WaitUntil(() => TagInteractor.TryResolve(tag, out _, out _), 5f);
    } catch (TimeoutException) {
      throw new InvalidOperationException(TagInteractor.DescribeMiss(tag));
    }

    if (!TagInteractor.TryResolve(tag, out Rect rect, out string? error)) {
      throw new InvalidOperationException(error ?? "Failed to resolve tag");
    }

    if (!XdoInput.Available) {
      throw new InvalidOperationException(
          "xdotool is not available; the docker image needs xdotool installed for mouse movement.");
    }

    XdoInput.MoveTo(rect.center);
    await WaitFrames(1);
  }

  public async Task PressKey(string key) {
    UnityEngine.KeyCode keyCode = MapKeyName(key);
    EventSynth.RequestKeyEvent(EventSynth.Mechanism.UIRootReinvoke, keyCode);
    await WaitFrames(2);
  }

  private static UnityEngine.KeyCode MapKeyName(string keyName) {
    return keyName.ToLowerInvariant() switch {
      "escape" => UnityEngine.KeyCode.Escape,
      "return" or "enter" => UnityEngine.KeyCode.Return,
      "space" => UnityEngine.KeyCode.Space,
      "tab" => UnityEngine.KeyCode.Tab,
      "delete" => UnityEngine.KeyCode.Delete,
      "backspace" => UnityEngine.KeyCode.Backspace,
      _ when keyName.Length == 1 && char.IsLetter(keyName[0]) =>
          (UnityEngine.KeyCode)Enum.Parse(typeof(UnityEngine.KeyCode), keyName, ignoreCase: true),
      _ when keyName.Length == 1 && char.IsDigit(keyName[0]) =>
          keyName[0] switch {
            '0' => UnityEngine.KeyCode.Alpha0,
            '1' => UnityEngine.KeyCode.Alpha1,
            '2' => UnityEngine.KeyCode.Alpha2,
            '3' => UnityEngine.KeyCode.Alpha3,
            '4' => UnityEngine.KeyCode.Alpha4,
            '5' => UnityEngine.KeyCode.Alpha5,
            '6' => UnityEngine.KeyCode.Alpha6,
            '7' => UnityEngine.KeyCode.Alpha7,
            '8' => UnityEngine.KeyCode.Alpha8,
            '9' => UnityEngine.KeyCode.Alpha9,
            _ => throw new ArgumentException($"Unknown key: {keyName}"),
          },
      _ => throw new ArgumentException(
          $"Unknown key: {keyName}; supported keys: Escape, Return, Enter, Space, Tab, Delete, Backspace, A-Z, 0-9"),
    };
  }

  public void Attach(string name, string content) {
    attachments.Add((name, content));
  }
}
