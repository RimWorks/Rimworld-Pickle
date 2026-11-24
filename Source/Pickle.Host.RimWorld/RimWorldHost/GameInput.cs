using System;
using System.Threading.Tasks;
using RimWorks.Pickle.Hosting;
using RimWorks.Pickle.Input;
using RimWorks.Pickle.Runtime;
using UnityEngine;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class GameInput : IGameInput {
  public void StartTagRecording() {
    TagStore.SessionActive = true;
  }

  public void CancelPending() {
    InteractionRequest.Clear();
  }

  public async Task Click(string tag, object? scope) {
    Rect rect = await ResolveTag(tag, scope);

    InteractionRequest.ArmClick(rect);
    try {
      await PickleDriver.Instance.WaitUntil(() => InteractionRequest.ClickFired, 2f, scope);
    } catch (TimeoutException) {
      InteractionRequest.Clear();
      throw new InvalidOperationException(
          $"tag '{tag}' resolved to {rect} but no button there took the click; "
          + "the element has to route through Widgets.ButtonInvisible for a click step to reach it");
    }

    await PickleDriver.Instance.WaitFrames(2, scope);
  }

  public async Task Hover(string tag, object? scope) {
    Rect rect = await ResolveTag(tag, scope);

    InteractionRequest.SetHover(rect);
    await PickleDriver.Instance.WaitFrames(1, scope);
  }

  public async Task PressKey(string key, object? scope) {
    EventSynth.RequestKeyEvent(EventSynth.Mechanism.UIRootReinvoke, KeyNames.Parse(key));
    await PickleDriver.Instance.WaitFrames(2, scope);

    if (EventSynth.TryTakeFailure(out Exception? failure)) {
      throw new InvalidOperationException($"key '{key}' could not be delivered", failure);
    }
  }

  private static async Task<Rect> ResolveTag(string tag, object? scope) {
    try {
      await PickleDriver.Instance.WaitUntil(() => TagInteractor.TryResolve(tag, out _, out _), 5f, scope);
    } catch (TimeoutException) {
      throw new InvalidOperationException(TagInteractor.DescribeMiss(tag));
    }

    if (!TagInteractor.TryResolve(tag, out Rect rect, out string? error)) {
      throw new InvalidOperationException(error ?? "Failed to resolve tag");
    }

    return rect;
  }
}
