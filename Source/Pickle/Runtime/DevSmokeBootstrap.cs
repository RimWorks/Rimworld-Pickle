using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

[StaticConstructorOnStartup]
public static class DevSmokeBootstrap {
  private const string LoadingEvent = "LoadingLongEvent";
  private const string SmokeFailed = "dev smoke failed";

  static DevSmokeBootstrap() {
    string? marker = Environment.GetEnvironmentVariable("MARKER");
    if (marker == "pump smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunQuickTestSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "fixture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunFixtureSmokeTest(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "run session smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunRunSessionSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "runner window smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunRunnerWindowSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "event synth smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunEventSynthSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "widget capture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunWidgetCaptureSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "tag store smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunTagStoreSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "tag click smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunTagClickSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "evidence smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunEvidenceSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "save fixture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunSaveFixtureSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "fixture manager smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunFixtureManagerSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "suite passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunSuite(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
    }
  }

  private static async Task RunSaveFixtureSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);
      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      _ = SaveFixtureSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunQuickTestSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      _ = PumpSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunFixtureSmokeTest() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      _ = FixtureSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunRunSessionSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      _ = RunSessionSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunRunnerWindowSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      RunnerWindowSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunEventSynthSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      EventSynthSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunWidgetCaptureSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      WidgetCaptureSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunTagStoreSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      _ = TagStoreSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunTagClickSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      _ = TagClickSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunEvidenceSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      _ = EvidenceSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  // Stays at the main menu rather than loading the play scene: a cold start with no game
  // is exactly where you go looking for a fixture to load.
  private static async Task RunFixtureManagerSmoke() {
    try {
      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Entry && Find.WindowStack != null, 180f);
      await driver.WaitFrames(5);

      _ = FixtureManagerSmoke.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }

  private static async Task RunSuite() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      await SuiteRunner.Run();
    } catch (Exception ex) {
      Log.ErrorTo("Pickle", ex, SmokeFailed);
    }
  }
}
