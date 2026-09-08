using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using RimWorks.Pickle.Autorun;
using RimWorks.Pickle.Core.Discovery;
using RimWorks.Pickle.Core.Fixtures;
using RimWorks.Pickle.Fixtures;
using RimWorks.Pickle.Runtime;
using RimWorks.Pickle.UI;
using Verse;

namespace RimWorks.Pickle.Web;

/// <summary>The dashboard's fixture routes: saving, loading, renaming and deleting fixtures, plus the catalogue used to list them.</summary>
public static class FixtureCommands {
  /// <summary>True while a fixture operation is running. The dashboard and step console both refuse to start while this is set.</summary>
  public static bool IsBusy { get; private set; }

  /// <summary>Runs a fixture action, if any, then returns the current fixture catalogue for every discovered mod.</summary>
  /// <param name="action">The action to run: <c>"save"</c>, <c>"load"</c>, <c>"rename"</c> or <c>"delete"</c>, or <c>null</c> to only read the catalogue.</param>
  /// <param name="suitePath">The fixtures directory of the mod the action targets.</param>
  /// <param name="name">The fixture name the action targets.</param>
  /// <param name="newName">The new name for a <c>"rename"</c> action.</param>
  /// <param name="overwrite">Whether a <c>"save"</c> action may replace an existing fixture.</param>
  /// <returns>The fixture catalogue as JSON.</returns>
  public static Task<string> Request(string? action, string? suitePath, string? name, string? newName, bool overwrite) {
    TaskCompletionSource<string> completion = new TaskCompletionSource<string>();
    if (!PickleDriver.Exists) {
      completion.SetException(new InvalidOperationException("The game is still loading."));
      return completion.Task;
    }

    PickleDriver.Post(async () => {
      try {
        if (action != null) {
          await Execute(action, suitePath, name, newName, overwrite);
        }

        completion.SetResult(BuildCatalog());
      } catch (Exception ex) {
        completion.SetException(ex);
      }
    });
    return completion.Task;
  }

  /// <summary>Runs one fixture action against a discovered mod's fixtures directory. Refuses while a run or another fixture operation is in progress.</summary>
  /// <param name="action">The action to run: <c>"save"</c>, <c>"load"</c>, <c>"rename"</c> or <c>"delete"</c>.</param>
  /// <param name="suitePath">The fixtures directory of the mod the action targets.</param>
  /// <param name="name">The fixture name the action targets.</param>
  /// <param name="newName">The new name for a <c>"rename"</c> action.</param>
  /// <param name="overwrite">Whether a <c>"save"</c> action may replace an existing fixture.</param>
  internal static async Task Execute(string action, string? suitePath, string? name, string? newName, bool overwrite) {
    if (IsBusy || AutorunState.IsAutorunning || RunnerWindow.Instance.IsRunning) {
      throw new InvalidOperationException("Wait for the current run or fixture operation to finish.");
    }

    DiscoveredSuite suite = SuiteScanner.DiscoverSuites().FirstOrDefault(candidate => candidate.FixturesDir == suitePath)
        ?? throw new ArgumentException("Select a discovered mod.", nameof(suitePath));
    string target = FixtureCatalog.PathForName(suite.WritableFixturesDir, name ?? string.Empty);
    IsBusy = true;
    try {
      if (action == "save") {
        if (File.Exists(target) && !overwrite) {
          throw new IOException("This fixture already exists. Confirm overwrite before saving.");
        }

        if (!SaveFixtureDialog.Save(suite, name!, overwrite)) {
          throw new IOException("The fixture was not saved. Check the game log.");
        }

        return;
      }

      FixtureEntry entry = FixtureCatalog.Read(suite.FixturesDir, suite.WritableFixturesDir)
          .FirstOrDefault(candidate => !candidate.IsShadowed && candidate.Name == name)
          ?? throw new FileNotFoundException("The fixture no longer exists. Refresh the list.");
      switch (action) {
        case "load":
          await FixtureLoader.LoadFixture(entry.FullPath, PickleDriver.Instance);
          break;
        case "rename":
          string renamed = FixtureCatalog.PathForName(Path.GetDirectoryName(entry.FullPath)!, newName ?? string.Empty);
          if (renamed != entry.FullPath) {
            File.Move(entry.FullPath, renamed);
          }

          break;
        case "delete":
          File.Delete(entry.FullPath);
          break;
        default:
          throw new ArgumentException("Unknown fixture action.", nameof(action));
      }
    } finally {
      IsBusy = false;
      RunnerWindow.Instance.PublishSnapshot();
    }
  }

  private static string BuildCatalog() {
    List<string> groups = [];
    foreach (DiscoveredSuite suite in SuiteScanner.DiscoverSuites()) {
      IEnumerable<string> fixtures = FixtureCatalog.Read(suite.FixturesDir, suite.WritableFixturesDir)
          .Where(entry => !entry.IsShadowed).Select(entry => {
            FixtureHeader header = FixtureHeader.Read(entry.FullPath);
            return "{\"name\":" + Json.Quote(entry.Name)
                + ",\"path\":" + Json.Quote(entry.FullPath)
                + ",\"recorded\":" + (entry.IsRecorded ? "true" : "false")
                + ",\"shadowedPath\":" + Json.Quote(entry.ShadowedPath)
                + ",\"sizeBytes\":" + entry.SizeBytes
                + ",\"modified\":" + Json.Quote(entry.Modified.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture))
                + ",\"gameVersion\":" + Json.Quote(header.GameVersion)
                + ",\"scenarioName\":" + Json.Quote(header.ScenarioName) + "}";
          });
      groups.Add("{\"id\":" + Json.Quote(suite.FixturesDir) + ",\"mod\":" + Json.Quote(suite.ModName)
          + ",\"directory\":" + Json.Quote(suite.WritableFixturesDir) + ",\"fixtures\":" + Json.Array(fixtures) + "}");
    }

    return "{\"canSave\":" + (Current.Game != null && Current.ProgramState == ProgramState.Playing ? "true" : "false")
        + ",\"busy\":" + (IsBusy || AutorunState.IsAutorunning || RunnerWindow.Instance.IsRunning ? "true" : "false")
        + ",\"suites\":" + Json.Array(groups) + "}";
  }
}
