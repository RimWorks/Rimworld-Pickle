using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace RimWorks.Pickle.Game.Tests;

public sealed class GameSession : IDisposable {
  private const int FirstPort = 27750;

  private const int PortCount = 10;

  private static readonly TimeSpan BootTimeout = TimeSpan.FromMinutes(6);

  private static readonly TimeSpan PollDelay = TimeSpan.FromSeconds(3);

  private readonly HttpClient http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
  private readonly string? gamecrateProfile;
  private readonly Process? process;

  private GameSession(int port, string? gamecrateProfile, Process? process) {
    Port = port;
    this.gamecrateProfile = gamecrateProfile;
    this.process = process;
  }

  public int Port { get; }

  public static GameSession Start() {
    int? running = FindControllablePort();
    if (running != null) {
      return new GameSession(running.Value, null, null);
    }

    string? profile = Environment.GetEnvironmentVariable("PICKLE_TEST_GAMECRATE");
    if (!string.IsNullOrWhiteSpace(profile)) {
      Run("gamecrate", $"run {profile} --detach --network host --build");
      return new GameSession(WaitForControllable(), profile, null);
    }

    string? gamePath = Environment.GetEnvironmentVariable("PICKLE_TEST_GAME");
    if (!string.IsNullOrWhiteSpace(gamePath)) {
      Process started = Launch(gamePath);
      return new GameSession(WaitForControllable(), null, started);
    }

    throw new InvalidOperationException(
        $"no game on {FirstPort}-{FirstPort + PortCount - 1}; set PICKLE_TEST_GAMECRATE or PICKLE_TEST_GAME");
  }

  public string? RunScenario(string gherkin) {
    using StringContent body = new StringContent(gherkin, Encoding.UTF8, "text/plain");
    using HttpResponseMessage response = http.PostAsync($"http://127.0.0.1:{Port}/gherkin", body)
        .GetAwaiter().GetResult();
    response.EnsureSuccessStatusCode();

    using StreamReader reader = new StreamReader(response.Content.ReadAsStream());
    List<string> failures = new List<string>();
    int failed = 0;
    bool finished = false;

    while (reader.ReadLine() is string line) {
      if (line.Length == 0) {
        continue;
      }

      using JsonDocument doc = JsonDocument.Parse(line);
      JsonElement root = doc.RootElement;
      switch (Text(root, "event")) {
        case "step":
          if (Text(root, "status") is string status && status != "passed" && status != "skipped") {
            failures.Add($"{Text(root, "keyword")}{Text(root, "text")}: {status} {Text(root, "failureMessage")}".Trim());
          }

          break;
        case "error":
          failures.Add(Text(root, "message") ?? "error");
          failed++;
          break;
        case "run-finished":
          failed += root.GetProperty("failed").GetInt32();
          finished = true;
          break;
        default:
          break;
      }
    }

    if (!finished) {
      failures.Add("the stream ended before run-finished");
      failed++;
    }

    return failed == 0 ? null : string.Join("\n", failures);
  }

  public void Dispose() {
    http.Dispose();

    if (gamecrateProfile != null) {
      Run("gamecrate", $"stop {gamecrateProfile}");
    }

    if (process != null && !process.HasExited) {
      process.Kill(true);
      process.WaitForExit(30_000);
    }

    process?.Dispose();
  }

  private static string? Text(JsonElement root, string name) {
    return root.TryGetProperty(name, out JsonElement value) && value.ValueKind == JsonValueKind.String
        ? value.GetString()
        : null;
  }

  private static int WaitForControllable() {
    DateTime deadline = DateTime.UtcNow + BootTimeout;
    while (DateTime.UtcNow < deadline) {
      int? port = FindControllablePort();
      if (port != null) {
        return port.Value;
      }

      Thread.Sleep(PollDelay);
    }

    throw new TimeoutException($"no controllable game after {BootTimeout.TotalMinutes} minutes");
  }

  private static int? FindControllablePort() {
    using HttpClient probe = new HttpClient { Timeout = TimeSpan.FromSeconds(3) };
    foreach (int port in Ports()) {
      try {
        string state = probe.GetStringAsync($"http://127.0.0.1:{port}/state").GetAwaiter().GetResult();
        if (state.Contains("\"controllable\":true", StringComparison.Ordinal)) {
          return port;
        }
      } catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException) {
        continue;
      }
    }

    return null;
  }

  private static IEnumerable<int> Ports() {
    string? named = Environment.GetEnvironmentVariable("PICKLE_TEST_PORT");
    if (int.TryParse(named, out int port)) {
      yield return port;
      yield break;
    }

    for (int i = 0; i < PortCount; i++) {
      yield return FirstPort + i;
    }
  }

  private static Process Launch(string gamePath) {
    ProcessStartInfo info = new ProcessStartInfo(gamePath) {
      WorkingDirectory = Path.GetDirectoryName(gamePath),
      UseShellExecute = false,
    };

    info.ArgumentList.Add($"-pickle-http-port={Ports().First()}");
    info.ArgumentList.Add("-pickle-no-browser");
    return Process.Start(info) ?? throw new InvalidOperationException($"{gamePath} did not start");
  }

  private static void Run(string file, string arguments) {
    using Process? run = Process.Start(new ProcessStartInfo(file, arguments) { UseShellExecute = false });
    run?.WaitForExit();
    if (run != null && run.ExitCode != 0) {
      throw new InvalidOperationException($"{file} {arguments} exited {run.ExitCode}");
    }
  }
}
