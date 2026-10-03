using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorks.Pickle.Core.Run;
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
  private const string ErrorPrefix = "{\"error\":";
  private const string OffValue = "false";
  private const string PlainText = "text/plain";
  private const string NdjsonContentType = "application/x-ndjson";

  private const string EvidencePrefix = "/screenshots/";

  private const int DefaultPort = 27750;

  // How many consecutive ports a dashboard with no requested port tries, starting at DefaultPort.
  private const int FallbackPortCount = 10;

  private static readonly string[] ReportFiles = ["junit.xml", "messages.ndjson", "summary.json", "summary.md"];

  private static readonly string[] MutatingPaths = ["/abort", "/pause", "/continue", "/run", "/scope", "/select", "/filter", "/mode", "/wip", "/break", "/pill", "/fixture", "/step", "/step/reset", "/gherkin"];

  // Every route here does its work and answers OkBody, so they share one lookup rather
  // than eleven branches in Route.
  private static readonly Dictionary<string, Action<HttpListenerContext>> Commands = new Dictionary<string, Action<HttpListenerContext>> {
    ["/abort"] = _ => RunnerCommands.Abort().GetAwaiter().GetResult(),
    ["/run"] = c => RunnerCommands.Run(c.Request.QueryString["scope"] ?? "all").GetAwaiter().GetResult(),
    ["/continue"] = _ => RunnerCommands.Continue().GetAwaiter().GetResult(),
    ["/pause"] = _ => RunnerCommands.Pause().GetAwaiter().GetResult(),
    ["/scope"] = c => RunnerCommands.SetScope(c.Request.QueryString["value"] ?? "all").GetAwaiter().GetResult(),
    ["/filter"] = Filter,
    ["/select"] = Select,
    ["/mode"] = c => RunnerCommands.SetMode(c.Request.QueryString["value"] ?? "watch").GetAwaiter().GetResult(),
    ["/wip"] = c => RunnerCommands.SetIncludeWip(c.Request.QueryString["on"] != OffValue).GetAwaiter().GetResult(),
    ["/pill"] = c => RunnerCommands.SetShowRunPill(c.Request.QueryString["on"] != OffValue).GetAwaiter().GetResult(),
    ["/break"] = c => RunnerCommands.SetBreakOnFailure(c.Request.QueryString["on"] != OffValue).GetAwaiter().GetResult(),
  };

  private static bool publishFailureLogged;
  private static HttpListener? listener;
  private static volatile bool running;

  // Built and published on the main thread; the listener thread only ever reads
  // this reference, so it never walks a collection while the run mutates it.
  private static volatile string snapshot = "{\"status\":\"idle\",\"features\":[]}";

  /// <summary>The session an autorun is driving, or <c>null</c> outside an autorun. Progress and abort requests route through it.</summary>
  public static RunSession? ActiveSession { get; set; }

  /// <summary>True once <see cref="Start"/> has a listener up. False after <see cref="Stop"/> or a failed start.</summary>
  public static bool IsRunning => running;

  /// <summary>The port the listener bound, or zero before <see cref="Start"/> succeeds.</summary>
  public static int Port { get; private set; }

  /// <summary>Replaces the snapshot the <c>/state</c> route serves.</summary>
  /// <param name="json">The full snapshot document, already serialized.</param>
  public static void Publish(string json) {
    snapshot = json;
  }

  /// <summary>Builds a snapshot and publishes it, absorbing anything the build throws.</summary>
  /// <param name="build">Builds the snapshot JSON from live state.</param>
  // The dashboard is a view of a run, not a part of it, so nothing it does may end one. A build
  // reads live game state and can meet that state mid-change, and the throw used to travel out
  // through OnProgress and be reported against whichever scenario was running. Every publisher
  // routes through here, so the runner window and the autorun path are both covered. Only the
  // first failure in a streak is logged, since the cause repeats every frame.
  public static void PublishSafely(Func<string> build) {
    try {
      Publish(build());
      publishFailureLogged = false;
    } catch (Exception ex) {
      if (publishFailureLogged) {
        return;
      }

      publishFailureLogged = true;
      Log.WarnTo(PickleLog.Channel, ex, "dashboard snapshot failed, the run continues without it");
    }
  }

  /// <summary>Starts the dashboard on the configured or default port, unless <c>-pickle-no-http</c> was passed, then opens it in a browser.</summary>
  // On unless asked otherwise. The old -pickle-http is gone; RimWorld ignores an argument
  // nothing reads, so a command line that still passes it keeps working.
  // A port given with -pickle-http-port is used as given, with no search: whoever names a
  // port needs that one. Without it, a taken default port moves the dashboard to the next
  // free one instead of leaving it dead, which would also leave no driver for the console
  // routes (DashboardSeed only creates it once the server is up).
  public static void StartUnlessDisabled() {
    if (GenCommandLine.CommandLineArgPassed("-pickle-no-http")) {
      return;
    }

    bool valued = GenCommandLine.TryGetCommandLineArg("-pickle-http-port", out string portValue);
    if (valued) {
      Start(int.TryParse(portValue, out int parsed) ? parsed : DefaultPort);
    } else {
      StartOnFreePort(DefaultPort);
    }

    OpenInBrowser(Port);
  }

  /// <summary>Starts the HTTP listener on a background thread. Does nothing if it is already running; logs and gives up if the port cannot be bound.</summary>
  /// <param name="port">The TCP port to listen on.</param>
  public static void Start(int port) {
    if (!TryStart(port, out Exception? error)) {
      Log.ErrorTo(PickleLog.Channel, error!, $"dashboard failed to start on port {port}");
    }
  }

  /// <summary>Stops the listener and releases its socket.</summary>
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

  private static void StartOnFreePort(int firstPort) {
    Exception? error = null;
    for (int port = firstPort; port < firstPort + FallbackPortCount; port++) {
      if (TryStart(port, out error)) {
        if (port != firstPort) {
          Log.InfoTo(PickleLog.Channel, "dashboard port {Taken} is in use, serving on port {Port} instead", [firstPort, port]);
        }

        return;
      }
    }

    Log.ErrorTo(PickleLog.Channel, error!, $"dashboard failed to start on ports {firstPort} to {firstPort + FallbackPortCount - 1}");
  }

  private static bool TryStart(int port, out Exception? error) {
    error = null;
    if (running) {
      return true;
    }

    try {
      listener = new HttpListener();
      listener.Prefixes.Add($"http://*:{port}/");
      listener.Start();
      running = true;
      Port = port;

      Thread worker = new Thread(Serve) { IsBackground = true, Name = "pickle-http" };
      worker.Start();

      Log.InfoTo(PickleLog.Channel, "dashboard on http://0.0.0.0:{Port}/", [port]);
      return true;
    } catch (Exception ex) {
      running = false;
      error = ex;
      try {
        listener?.Close();
      } catch (Exception) {
        // a listener that never started has nothing to release
      }

      listener = null;
      return false;
    }
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
      Log.WarnTo(PickleLog.Channel, ex, "could not open the dashboard in a browser");
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
      Log.ErrorTo(PickleLog.Channel, ex, "dashboard request failed");
      context.Response.StatusCode = 400;
      Write(context, JsonContentType, ErrorPrefix + Json.Quote(ex.Message) + "}");
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
    if (Rejected(context, path)) {
      return;
    }

    if (Commands.TryGetValue(path, out Action<HttpListenerContext> command)) {
      command(context);
      Write(context, JsonContentType, OkBody);
      return;
    }

    Dispatch(context, path);
  }

  // The listener binds 0.0.0.0, so a page in any browser on the network could abort a run
  // through an <img> tag if a mutating route answered a GET.
  private static bool Rejected(HttpListenerContext context, string path) {
    if (Array.IndexOf(MutatingPaths, path) < 0) {
      return false;
    }

    string? origin = context.Request.Headers["Origin"];
    if (origin != null
        && !string.Equals(origin, context.Request.Url.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase)) {
      context.Response.StatusCode = 403;
      Write(context, PlainText, "use the dashboard origin");
      return true;
    }

    if (!string.Equals(context.Request.HttpMethod, "POST", StringComparison.Ordinal)) {
      context.Response.StatusCode = 405;
      context.Response.AddHeader("Allow", "POST");
      Write(context, PlainText, "use POST");
      return true;
    }

    return false;
  }

  private static void Dispatch(HttpListenerContext context, string path) {
    switch (path) {
      case "/state":
        Write(context, JsonContentType, snapshot);
        return;
      case "/fixtures":
      case "/fixture":
        ServeFixtures(context, path);
        return;
      case "/steps":
      case "/step":
      case "/step/reset":
        ServeConsole(context, path);
        return;
      case "/gherkin":
        ServeGherkin(context);
        return;
      case "/gherkin/runs":
        Write(context, JsonContentType, GherkinCommands.Recent());
        return;
      case "/":
        Write(context, "text/html; charset=utf-8", Dashboard.Html);
        return;
      case "/report":
        ServeReport(context);
        return;
      default:
        ServeFile(context, path);
        return;
    }
  }

  private static void ServeFile(HttpListenerContext context, string path) {
    if (path.StartsWith("/reports/", StringComparison.Ordinal) && Array.IndexOf(ReportFiles, path.Substring(9)) >= 0) {
      string name = path.Substring(9);
      string file = Path.Combine(ScreenshotCapture.ReportRoot(), name);
      if (!File.Exists(file)) {
        context.Response.StatusCode = 404;
        Write(context, PlainText, "no report yet, run something first");
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
    Write(context, PlainText, "not found");
  }

  private static void ServeFixtures(HttpListenerContext context, string path) {
    try {
      string catalog = FixtureCommands.Request(
          path == "/fixture" ? context.Request.QueryString["action"] ?? string.Empty : null,
          context.Request.QueryString["suite"], context.Request.QueryString["name"],
          context.Request.QueryString["newName"], context.Request.QueryString["overwrite"] == "true").GetAwaiter().GetResult();
      Write(context, JsonContentType, catalog);
    } catch (Exception ex) {
      context.Response.StatusCode = 400;
      Write(context, JsonContentType, ErrorPrefix + Json.Quote(ex.Message) + "}");
    }
  }

  private static void Filter(HttpListenerContext context) {
    RunnerCommands.Filter(
        context.Request.QueryString["search"], context.Request.QueryString["mod"], context.Request.QueryString["tag"],
        context.Request.QueryString["additive"] == "true", context.Request.QueryString["clearTags"] == "true").GetAwaiter().GetResult();
  }

  private static void Select(HttpListenerContext context) {
    string? scope = context.Request.QueryString["scope"];
    bool on = context.Request.QueryString["on"] != "false";

    if (scope != null) {
      if (scope != "all" && scope != "none") {
        throw new ArgumentException("Unknown selection scope.");
      }

      RunnerCommands.SelectAll(scope == "all").GetAwaiter().GetResult();
      return;
    }

    if (int.TryParse(context.Request.QueryString["index"], out int index)) {
      RunnerCommands.Select(context.Request.QueryString["path"] ?? string.Empty, index, on).GetAwaiter().GetResult();
      return;
    }

    if (context.Request.QueryString["index"] != null
        || (context.Request.QueryString["path"] == null && context.Request.QueryString["mod"] == null)) {
      throw new ArgumentException("Select a discovered scenario, feature, or mod.");
    }

    RunnerCommands.SelectAll(on, context.Request.QueryString["path"], context.Request.QueryString["mod"]).GetAwaiter().GetResult();
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
      Write(context, JsonContentType, ErrorPrefix + Json.Quote(ex.Message) + "}");
    } catch (Exception ex) {
      context.Response.StatusCode = 400;
      Write(context, JsonContentType, ErrorPrefix + Json.Quote(ex.Message) + "}");
    }
  }

  private static void ServeGherkin(HttpListenerContext context) {
    string gherkin;
    using (StreamReader reader = new StreamReader(context.Request.InputStream, context.Request.ContentEncoding)) {
      gherkin = reader.ReadToEnd();
    }

    context.Response.ContentType = NdjsonContentType;
    context.Response.SendChunked = true;

    void WriteChunk(string line) {
      byte[] bytes = Encoding.UTF8.GetBytes(line + "\n");
      context.Response.OutputStream.Write(bytes, 0, bytes.Length);
      context.Response.OutputStream.Flush();
    }

    try {
      GherkinCommands.Stream(gherkin, WriteChunk);
    } catch (Exception ex) {
      WriteChunk(RunEvent.Error(ex.Message));
    }
  }

  // The last run's report, whoever wrote it. The dashboard opens this in a tab when a run
  // ends, so a 404 here means the run wrote nothing rather than that the route is wrong.
  private static void ServeReport(HttpListenerContext context) {
    string file = Path.Combine(ScreenshotCapture.ReportRoot(), "report.html");

    if (!File.Exists(file)) {
      context.Response.StatusCode = 404;
      Write(context, PlainText, "no report yet, run something first");
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
      Write(context, PlainText, "bad path");
      return;
    }

    if (!full.StartsWith(root + Path.DirectorySeparatorChar, StringComparison.Ordinal) || !File.Exists(full)) {
      context.Response.StatusCode = 404;
      Write(context, PlainText, "not found");
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
