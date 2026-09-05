using System;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorks.Pickle.Evidence;
using RimWorks.Pickle.Run;
using UnityEngine;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Web;

/// <summary>
/// Serves the run dashboard over HTTP, in every launch mode including headless. The
/// listener thread only reads a published snapshot, never a live collection.
/// </summary>
public static class PickleHttpServer {
  private const string JsonContentType = "application/json";
  private const string OkBody = "{\"ok\":true}";

  private const string EvidencePrefix = "/screenshots/";

  private const int DefaultPort = 27750;

  private static readonly string[] ReportFiles = ["junit.xml", "messages.ndjson", "summary.json", "summary.md"];

  private static readonly string[] MutatingPaths = ["/abort", "/pause", "/continue", "/run", "/scope", "/select", "/filter", "/mode", "/wip", "/break", "/pill", "/fixture", "/step", "/step/reset"];

  private static HttpListener? listener;
  private static volatile bool running;

  // Built and published on the main thread; the listener thread only ever reads
  // this reference, so it never walks a collection while the run mutates it.
  private static volatile string snapshot = "{\"status\":\"idle\",\"features\":[]}";

  public static RunSession? ActiveSession { get; set; }

  public static bool IsRunning => running;

  public static void Publish(string json) {
    snapshot = json;
  }

  // On unless asked otherwise. The old -pickle-http is gone; RimWorld ignores an argument
  // nothing reads, so a command line that still passes it keeps working.
  public static void StartUnlessDisabled() {
    if (GenCommandLine.CommandLineArgPassed("-pickle-no-http")) {
      return;
    }

    bool valued = GenCommandLine.TryGetCommandLineArg("-pickle-http-port", out string portValue);
    int port = valued && int.TryParse(portValue, out int parsed) ? parsed : DefaultPort;
    Start(port);
    OpenInBrowser(port);
  }

  public static void Start(int port) {
    if (running) {
      return;
    }

    try {
      listener = new HttpListener();
      listener.Prefixes.Add($"http://*:{port}/");
      listener.Start();
      running = true;

      Thread worker = new Thread(Serve) { IsBackground = true, Name = "pickle-http" };
      worker.Start();

      Log.Info("pickle: dashboard on http://0.0.0.0:{Port}/", [port]);
    } catch (Exception ex) {
      running = false;
      Log.Error(ex, $"pickle: dashboard failed to start on port {port}");
    }
  }

  public static void Stop() {
    running = false;
    try {
      listener?.Stop();
      listener?.Close();
    } catch {
      // shutting down anyway, and a listener that is already dead throws here
    }
    listener = null;
  }

  // Application.OpenURL picks the platform's own handler. Not on an autorun: that is CI or a
  // container, where there is no browser and nobody to look at it.
  private static void OpenInBrowser(int port) {
    if (!running
        || GenCommandLine.CommandLineArgPassed("-pickle-no-browser")
        || GenCommandLine.CommandLineArgPassed("-pickle-run")) {
      return;
    }

    try {
      Application.OpenURL($"http://localhost:{port}/");
    } catch (Exception ex) {
      Log.Warn(ex, "pickle: could not open the dashboard in a browser");
    }
  }

  private static void Serve() {
    while (running) {
      HttpListenerContext context;
      try {
        context = listener!.GetContext();
      } catch (Exception) {
        // Stop() closes the listener out from under GetContext; that is the exit path.
        return;
      }

      ThreadPool.QueueUserWorkItem(_ => Respond(context));
    }
  }

  private static void Respond(HttpListenerContext context) {
    try {
      Route(context);
    } catch (Exception ex) {
      Log.Error(ex, "pickle: dashboard request failed");
      context.Response.StatusCode = 400;
      Write(context, JsonContentType, "{\"error\":" + Json.Quote(ex.Message) + "}");
    } finally {
      try {
        context.Response.Close();
      } catch (Exception) {
        // the client may disconnect during a fixture load.
      }
    }
  }

  private static void Route(HttpListenerContext context) {
    string path = context.Request.Url.AbsolutePath;

    string? origin = context.Request.Headers["Origin"];
    if (Array.IndexOf(MutatingPaths, path) >= 0 && origin != null
        && !string.Equals(origin, context.Request.Url.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) {
      context.Response.StatusCode = 403;
      Write(context, "text/plain", "use the dashboard origin");
      return;
    }

    // The listener binds 0.0.0.0, so a page in any browser on the network could abort a run
    // through an <img> tag if these answered a GET. A wrong-method script also fails loudly
    // here rather than looking like a run that never started.
    if (Array.IndexOf(MutatingPaths, path) >= 0
        && !string.Equals(context.Request.HttpMethod, "POST", StringComparison.Ordinal)) {
      context.Response.StatusCode = 405;
      context.Response.AddHeader("Allow", "POST");
      Write(context, "text/plain", "use POST");
      return;
    }

    if (path == "/state") {
      Write(context, JsonContentType, snapshot);
      return;
    }

    if (path == "/fixtures" || path == "/fixture") {
      try {
        string catalog = FixtureCommands.Request(
            path == "/fixture" ? context.Request.QueryString["action"] ?? string.Empty : null,
            context.Request.QueryString["suite"], context.Request.QueryString["name"],
            context.Request.QueryString["newName"], context.Request.QueryString["overwrite"] == "true").GetAwaiter().GetResult();
        Write(context, JsonContentType, catalog);
      } catch (Exception ex) {
        context.Response.StatusCode = 400;
        Write(context, JsonContentType, "{\"error\":" + Json.Quote(ex.Message) + "}");
      }

      return;
    }

    if (path == "/steps" || path == "/step" || path == "/step/reset") {
      ServeConsole(context, path);
      return;
    }

    if (path == "/abort") {
      RunnerCommands.Abort().GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/run") {
      RunnerCommands.Run(context.Request.QueryString["scope"] ?? "all").GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/continue") {
      RunnerCommands.Continue().GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/pause") {
      RunnerCommands.Pause().GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/scope") {
      RunnerCommands.SetScope(context.Request.QueryString["value"] ?? "all").GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/filter") {
      RunnerCommands.Filter(
          context.Request.QueryString["search"], context.Request.QueryString["mod"], context.Request.QueryString["tag"],
          context.Request.QueryString["additive"] == "true", context.Request.QueryString["clearTags"] == "true").GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/select") {
      string? scope = context.Request.QueryString["scope"];
      bool on = context.Request.QueryString["on"] != "false";

      if (scope != null) {
        if (scope != "all" && scope != "none") {
          throw new ArgumentException("Unknown selection scope.");
        }

        RunnerCommands.SelectAll(scope == "all").GetAwaiter().GetResult();
      } else if (int.TryParse(context.Request.QueryString["index"], out int index)) {
        RunnerCommands.Select(context.Request.QueryString["path"] ?? string.Empty, index, on).GetAwaiter().GetResult();
      } else {
        if (context.Request.QueryString["index"] != null
            || (context.Request.QueryString["path"] == null && context.Request.QueryString["mod"] == null)) {
          throw new ArgumentException("Select a discovered scenario, feature, or mod.");
        }

        RunnerCommands.SelectAll(on, context.Request.QueryString["path"], context.Request.QueryString["mod"]).GetAwaiter().GetResult();
      }

      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/mode") {
      RunnerCommands.SetMode(context.Request.QueryString["value"] ?? "watch").GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/wip") {
      RunnerCommands.SetIncludeWip(context.Request.QueryString["on"] != "false").GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/pill") {
      RunnerCommands.SetShowRunPill(context.Request.QueryString["on"] != "false").GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/break") {
      RunnerCommands.SetBreakOnFailure(context.Request.QueryString["on"] != "false").GetAwaiter().GetResult();
      Write(context, JsonContentType, OkBody);
      return;
    }

    if (path == "/") {
      Write(context, "text/html; charset=utf-8", Dashboard.Html);
      return;
    }

    if (path == "/report") {
      ServeReport(context);
      return;
    }

    if (path.StartsWith("/reports/", StringComparison.Ordinal) && Array.IndexOf(ReportFiles, path.Substring(9)) >= 0) {
      string name = path.Substring(9);
      string file = Path.Combine(ScreenshotCapture.ReportRoot(), name);
      if (!File.Exists(file)) {
        context.Response.StatusCode = 404;
        Write(context, "text/plain", "no report yet, run something first");
      } else {
        context.Response.AddHeader("Content-Disposition", "attachment; filename=\"" + name + "\"");
        Write(context, ContentTypeFor(file), File.ReadAllBytes(file));
      }

      return;
    }

    if (path.StartsWith(EvidencePrefix, StringComparison.Ordinal)) {
      ServeEvidence(context, path.Substring(EvidencePrefix.Length));
      return;
    }

    context.Response.StatusCode = 404;
    Write(context, "text/plain", "not found");
  }

  // A console step runs arbitrary registered steps on request, so it takes the same
  // origin and POST guards the run routes take. A busy game answers 409, not a queue.
  private static void ServeConsole(HttpListenerContext context, string path) {
    try {
      Task<string> work = path switch {
        "/steps" => ConsoleCommands.Catalog(),
        "/step" => ConsoleCommands.Run(context.Request.QueryString["text"]),
        _ => ConsoleCommands.Reset(),
      };

      Write(context, JsonContentType, work.GetAwaiter().GetResult());
    } catch (InvalidOperationException ex) {
      context.Response.StatusCode = 409;
      Write(context, JsonContentType, "{\"error\":" + Json.Quote(ex.Message) + "}");
    } catch (Exception ex) {
      context.Response.StatusCode = 400;
      Write(context, JsonContentType, "{\"error\":" + Json.Quote(ex.Message) + "}");
    }
  }

  // The last run's report, whoever wrote it. The dashboard opens this in a tab when a run
  // ends, so a 404 here means the run wrote nothing rather than that the route is wrong.
  private static void ServeReport(HttpListenerContext context) {
    string file = Path.Combine(ScreenshotCapture.ReportRoot(), "report.html");

    if (!File.Exists(file)) {
      context.Response.StatusCode = 404;
      Write(context, "text/plain", "no report yet, run something first");
      return;
    }

    Write(context, "text/html; charset=utf-8", File.ReadAllText(file));
  }

  // In CI this listener sits behind a public tunnel for the length of a job, so a request
  // is only answered once the resolved file is known to sit inside the evidence tree.
  private static void ServeEvidence(HttpListenerContext context, string relative) {
    string root = Path.GetFullPath(ScreenshotCapture.ReportsDirectory());
    string full;

    try {
      full = Path.GetFullPath(Path.Combine(root, Uri.UnescapeDataString(relative)));
    } catch (Exception) {
      context.Response.StatusCode = 400;
      Write(context, "text/plain", "bad path");
      return;
    }

    if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(full)) {
      context.Response.StatusCode = 404;
      Write(context, "text/plain", "not found");
      return;
    }

    Write(context, ContentTypeFor(full), File.ReadAllBytes(full));
  }

  private static string ContentTypeFor(string path) {
    switch (Path.GetExtension(path).ToLowerInvariant()) {
      case ".jpg":
      case ".jpeg":
        return "image/jpeg";
      case ".png":
        return "image/png";
      case ".webm":
        return "video/webm";
      case ".html":
        return "text/html; charset=utf-8";
      case ".json":
        return JsonContentType;
      default:
        return "application/octet-stream";
    }
  }

  private static void Write(HttpListenerContext context, string contentType, string body) {
    Write(context, contentType, Encoding.UTF8.GetBytes(body));
  }

  private static void Write(HttpListenerContext context, string contentType, byte[] bytes) {
    context.Response.ContentType = contentType;
    context.Response.ContentLength64 = bytes.Length;
    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
  }
}
