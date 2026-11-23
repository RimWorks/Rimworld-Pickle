using System.Threading.Tasks;

namespace RimWorks.Pickle.Hosting;

/// <summary>
/// The host's input: a click or a hover on a tagged widget, and a key press by name. Each member
/// completes once the game has taken the input, so the next step sees its effect.
/// </summary>
public interface IGameInput {
  /// <summary>Starts recording where tagged widgets draw, so a click or a hover can find them.</summary>
  void StartTagRecording();

  /// <summary>Drops a click or a hover that a step asked for and no widget has taken yet.</summary>
  void CancelPending();

  /// <summary>Waits for a tagged widget to appear and clicks it.</summary>
  /// <param name="tag">The tag the widget was recorded under.</param>
  /// <param name="scope">The step's wait scope, so the waits are torn down with the step.</param>
  /// <returns>A task that completes once a widget has taken the click, faulted when the tag never
  /// appears or nothing there takes it.</returns>
  Task Click(string tag, object? scope);

  /// <summary>Waits for a tagged widget to appear and holds the pointer over it, without clicking.</summary>
  /// <param name="tag">The tag the widget was recorded under.</param>
  /// <param name="scope">The step's wait scope, so the waits are torn down with the step.</param>
  /// <returns>A task that completes once the widget reports the pointer as over it, faulted when
  /// the tag never appears.</returns>
  Task Hover(string tag, object? scope);

  /// <summary>Presses a key in the game's UI.</summary>
  /// <param name="key">The key's name, such as <c>Escape</c>, <c>Return</c> or <c>A</c>.</param>
  /// <param name="scope">The step's wait scope, so the waits are torn down with the step.</param>
  /// <returns>A task that completes once the game has processed the key, faulted when the name is
  /// unknown or the key could not be delivered.</returns>
  Task PressKey(string key, object? scope);
}
