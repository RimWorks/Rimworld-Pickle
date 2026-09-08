using System.IO;
using System.Reflection;

namespace RimWorks.Pickle.Web;

/// <summary>
/// The two bundles Dashboard/ builds: the live page and the report template. Embedded,
/// so nothing resolves from disk at runtime.
/// </summary>
public static class Dashboard {
  private const string DashboardResource = "Pickle.Dashboard.html";
  private const string ReportResource = "Pickle.ReportTemplate.html";

  /// <summary>The live dashboard page, read from the embedded bundle on first use and cached after.</summary>
  public static string Html => field ??= Read(DashboardResource);

  /// <summary>The standalone report template, read from the embedded bundle on first use and cached after.</summary>
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
