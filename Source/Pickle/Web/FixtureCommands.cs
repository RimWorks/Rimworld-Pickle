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

public static class FixtureCommands {
  public static bool IsBusy { get; private set; }

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
