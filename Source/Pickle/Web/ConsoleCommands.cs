using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Run;
using RimWorks.Pickle.Core.Steps;
using RimWorks.Pickle.Run;

namespace RimWorks.Pickle.Web;

/// <summary>
/// The console routes. Everything here runs on the driver pump, because resolving and
/// running a step touches RimWorld statics the listener thread must not.
/// </summary>
public static class ConsoleCommands {
  public static Task<string> Catalog() {
    return RunnerCommands.PostAsync(() => {
      StepConsole.EnsureLoaded();
      IEnumerable<string> steps = StepConsole.Definitions
          .OrderBy(definition => definition.Pattern, StringComparer.OrdinalIgnoreCase)
          .Select(definition => "{\"pattern\":" + Json.Quote(definition.Pattern)
              + ",\"kind\":" + Json.Quote(definition.Kind.ToString())
              + ",\"source\":" + Json.Quote(definition.Source) + "}");

      return Task.FromResult("{\"steps\":" + Json.Array(steps) + "}");
    });
  }

  public static Task<string> Run(string? text) {
    return RunnerCommands.PostAsync(async () => {
      StepConsole.RefuseWhenBusy();
      (StepResult result, List<(string Source, string Content)> dumps) = await StepConsole.Run(text ?? string.Empty);
      return Build(result, dumps);
    });
  }

  public static Task<string> Reset() {
    return RunnerCommands.PostAsync(() => {
      StepConsole.RefuseWhenBusy();
      StepConsole.Reset();
      return Task.FromResult("{\"ok\":true}");
    });
  }

  // An undefined step already carries its generated C# stub in FailureMessage, so it is
  // lifted into its own field rather than shown to the user as an error string.
  private static string Build(StepResult result, List<(string Source, string Content)> dumps) {
    bool undefined = result.Status == StepStatus.Undefined;
    IEnumerable<string> stateDumps = dumps
        .Select(dump => "{\"source\":" + Json.Quote(dump.Source) + ",\"content\":" + Json.Quote(dump.Content) + "}");

    return "{\"keyword\":" + Json.Quote(result.Keyword.Trim())
        + ",\"text\":" + Json.Quote(result.Text)
        + ",\"status\":" + Json.Quote(result.Status.ToString())
        + ",\"durationMs\":" + Json.Number(result.DurationMs)
        + ",\"failureMessage\":" + Json.Quote(undefined ? null : result.FailureMessage)
        + ",\"skeleton\":" + Json.Quote(undefined ? result.FailureMessage : null)
        + ",\"stateDumps\":" + Json.Array(stateDumps) + "}";
  }
}
