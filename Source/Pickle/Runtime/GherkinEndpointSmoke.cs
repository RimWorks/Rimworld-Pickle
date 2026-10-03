using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using RimWorks.Pickle.Web;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>Selected by <c>MARKER="gherkin endpoint smoke passed"</c>, which is also the line it logs.</summary>
public static class GherkinEndpointSmoke {
  private const string Source = """
Feature: Gherkin Endpoint Smoke
  Scenario: Smoke step passes
    Given smoke step passes
""";

  /// <summary>Faults nothing, so the bootstrap can start it and walk away.</summary>
  /// <returns>A task that completes when the smoke finishes.</returns>
  public static async Task Run() {
    PickleContext ctx = new PickleContext();
    try {
      await RunAsync(ctx);
      Log.InfoTo(PickleLog.Channel, "gherkin endpoint smoke passed");
    } catch (Exception ex) {
      Log.ErrorTo(PickleLog.Channel, ex, "gherkin endpoint smoke failed");
    }
  }

  /// <summary>
  /// Posts the feature from a background thread, since the request blocks until the run it triggers
  /// has finished, then asserts the events the stream carried.
  /// </summary>
  /// <param name="ctx">The context to assert and wait against.</param>
  /// <returns>A task that completes when the smoke finishes.</returns>
  public static async Task RunAsync(PickleContext ctx) {
    ctx.Require(PickleHttpServer.IsRunning, "GherkinEndpointSmoke: the dashboard server is up");

    List<string> lines = new List<string>();
    Exception? failure = null;
    bool finished = false;

    Thread caller = new Thread(() => {
      try {
        Collect(lines);
      } catch (Exception ex) {
        failure = ex;
      } finally {
        finished = true;
      }
    }) { IsBackground = true, Name = "pickle-gherkin-smoke" };
    caller.Start();

    await ctx.WaitUntil(() => finished, 60f);

    if (failure != null) {
      throw new InvalidOperationException("GherkinEndpointSmoke: the request threw", failure);
    }

    string document = string.Join("\n", lines);
    ctx.Assert(lines.Count >= 5, $"GherkinEndpointSmoke: at least five events arrived, got {lines.Count}: {document}");
    ctx.Assert(lines[0].Contains("\"event\":\"run-started\""), $"GherkinEndpointSmoke: the first event is run-started, got {lines[0]}");
    ctx.Assert(document.Contains("\"event\":\"scenario-started\""), $"GherkinEndpointSmoke: a scenario-started arrived: {document}");
    ctx.Assert(document.Contains("\"status\":\"Passed\""), $"GherkinEndpointSmoke: the step passed: {document}");
    ctx.Assert(
        lines[lines.Count - 1].Contains("\"event\":\"run-finished\"") && lines[lines.Count - 1].Contains("\"passed\":1"),
        $"GherkinEndpointSmoke: the last event is run-finished with one pass, got {lines[lines.Count - 1]}");
  }

  private static void Collect(List<string> lines) {
    HttpWebRequest request = (HttpWebRequest)WebRequest.Create($"http://127.0.0.1:{PickleHttpServer.Port}/gherkin");
    request.Method = "POST";
    request.ContentType = "text/plain";
    request.Timeout = 60000;
    request.ReadWriteTimeout = 60000;

    byte[] body = Encoding.UTF8.GetBytes(Source);
    request.ContentLength = body.Length;
    using (Stream stream = request.GetRequestStream()) {
      stream.Write(body, 0, body.Length);
    }

    using WebResponse response = request.GetResponse();
    using Stream received = response.GetResponseStream();
    using StreamReader reader = new StreamReader(received, Encoding.UTF8);

    while (reader.ReadLine() is string line) {
      if (line.Length > 0) {
        lines.Add(line);
      }
    }
  }
}
