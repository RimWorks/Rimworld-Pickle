using System;
using System.Net;
using System.Text;
using System.Threading;
using Pickle.Run;
using Verse;

namespace Pickle.Web;

/// <summary>
/// Serves the run dashboard over HTTP, in every launch mode including headless. The
/// listener thread only reads a published snapshot, never a live collection.
/// </summary>
public static class PickleHttpServer {
  private const int DefaultPort = 27750;

  private static HttpListener? listener;
  private static Thread? worker;
  private static volatile bool running;

  // Built and published on the main thread; the listener thread only ever reads
  // this reference, so it never walks a collection while the run mutates it.
  private static volatile string snapshot = "{\"status\":\"idle\",\"features\":[]}";

  public static RunSession? ActiveSession { get; set; }

  public static bool IsRunning => running;

  public static void Publish(string json) {
    snapshot = json;
  }

  public static void StartIfRequested() {
    bool bare = GenCommandLine.CommandLineArgPassed("-pickle-http");
    bool valued = GenCommandLine.TryGetCommandLineArg("-pickle-http-port", out string portValue);
    if (!bare && !valued) {
      return;
    }

    int port = valued && int.TryParse(portValue, out int parsed) ? parsed : DefaultPort;
    Start(port);
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

      worker = new Thread(Serve) { IsBackground = true, Name = "pickle-http" };
      worker.Start();

      Log.Message($"pickle: dashboard on http://0.0.0.0:{port}/");
    } catch (Exception ex) {
      running = false;
      Log.Error($"pickle: dashboard failed to start on port {port}: {ex.Message}");
    }
  }

  public static void Stop() {
    running = false;
    try {
      listener?.Stop();
      listener?.Close();
    } catch { }
    listener = null;
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

      try {
        Route(context);
      } catch (Exception ex) {
        Log.Error($"pickle: dashboard request failed: {ex.Message}");
      } finally {
        try { context.Response.Close(); } catch { }
      }
    }
  }

  private static void Route(HttpListenerContext context) {
    string path = context.Request.Url.AbsolutePath;

    if (path == "/state") {
      Write(context, "application/json", snapshot);
      return;
    }

    if (path == "/abort") {
      RunnerCommands.Abort();
      Write(context, "application/json", "{\"ok\":true}");
      return;
    }

    if (path == "/run") {
      RunnerCommands.Run(context.Request.QueryString["scope"] ?? "all");
      Write(context, "application/json", "{\"ok\":true}");
      return;
    }

    if (path == "/select") {
      string? scope = context.Request.QueryString["scope"];
      bool on = context.Request.QueryString["on"] != "false";

      if (scope != null) {
        RunnerCommands.SelectAll(scope == "all");
      } else if (int.TryParse(context.Request.QueryString["index"], out int index)) {
        RunnerCommands.Select(context.Request.QueryString["path"] ?? string.Empty, index, on);
      }

      Write(context, "application/json", "{\"ok\":true}");
      return;
    }

    if (path == "/mode") {
      RunnerCommands.SetMode(context.Request.QueryString["value"] ?? "watch");
      Write(context, "application/json", "{\"ok\":true}");
      return;
    }

    if (path == "/break") {
      RunnerCommands.SetBreakOnFailure(context.Request.QueryString["on"] != "false");
      Write(context, "application/json", "{\"ok\":true}");
      return;
    }

    if (path == "/") {
      Write(context, "text/html; charset=utf-8", Dashboard.Html);
      return;
    }

    context.Response.StatusCode = 404;
    Write(context, "text/plain", "not found");
  }

  private static void Write(HttpListenerContext context, string contentType, string body) {
    byte[] bytes = Encoding.UTF8.GetBytes(body);
    context.Response.ContentType = contentType;
    context.Response.ContentLength64 = bytes.Length;
    context.Response.OutputStream.Write(bytes, 0, bytes.Length);
  }
}
