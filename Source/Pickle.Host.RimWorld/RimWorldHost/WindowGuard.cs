using RimWorks.Pickle.Hosting;
using RimWorks.Pickle.Runtime;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class WindowGuard : IWindowGuard {
  public void EndSuppression() {
    WindowSuppression.End();
  }
}
