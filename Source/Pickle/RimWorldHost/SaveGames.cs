using System.Threading.Tasks;
using RimWorks.Pickle.Fixtures;
using RimWorks.Pickle.Hosting;
using RimWorks.Pickle.Runtime;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class SaveGames : ISaveGames {
  public Task LoadSave(string path, object? scope) {
    return FixtureLoader.LoadFixture(path, PickleDriver.Instance, scope);
  }

  public Task SaveAndReload(string name, bool keepSave, object? scope) {
    return FixtureLoader.SaveAndReload(name, PickleDriver.Instance, scope, keepSave);
  }

  public Task StartQuickstart(string name, object? scope) {
    return FixtureLoader.LoadQuickstart(name, PickleDriver.Instance, scope);
  }
}
