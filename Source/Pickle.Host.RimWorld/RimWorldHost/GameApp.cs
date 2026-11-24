using System;
using System.Diagnostics;
using System.Threading;
using RimWorks.Pickle.Hosting;
using UnityEngine;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class GameApp : IGameApp {
  private const int QuitGraceMs = 15000;

  public string ConsoleLogPath => Application.consoleLogPath;

  public void Quit(int exitCode) {
    Thread killer = new Thread(() => {
      Thread.Sleep(QuitGraceMs);
      Console.Error.WriteLine("pickle: Application.Quit did not end the process, force-killing");
      Process.GetCurrentProcess().Kill();
    }) { IsBackground = true };

    killer.Start();
    Application.Quit(exitCode);
  }

  public void OpenUrl(string url) {
    Application.OpenURL(url);
  }
}
