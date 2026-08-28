using System.IO;
using System.Reflection;

namespace Pickle.Web;

/// <summary>
/// The two bundles Dashboard/ builds: the live page and the report template. Embedded,
/// so nothing resolves from disk at runtime.
/// </summary>
public static class Dashboard {
  private const string DashboardResource = "Pickle.Dashboard.html";
  private const string ReportResource = "Pickle.ReportTemplate.html";

  public static string Html => field ??= Read(DashboardResource);

  public static string ReportTemplate => field ??= Read(ReportResource);

  private static string Read(string resourceName) {
    using Stream? stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(resourceName);
    if (stream == null) {
      return "<!doctype html><title>Pickle</title><p>Bundle missing. Run <code>npm run build</code> in Dashboard/.";
    }

    using StreamReader reader = new StreamReader(stream);
    return reader.ReadToEnd();
  }
}
