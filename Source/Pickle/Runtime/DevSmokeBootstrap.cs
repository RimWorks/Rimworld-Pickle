using System;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using Verse;

namespace Pickle.Runtime;

[StaticConstructorOnStartup]
public static class DevSmokeBootstrap {
  private const string LoadingEvent = "LoadingLongEvent";

  static DevSmokeBootstrap() {
    string? marker = Environment.GetEnvironmentVariable("MARKER");
    if (marker == "pickle: pump smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunQuickTestSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: fixture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunFixtureSmokeTest(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: run session smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunRunSessionSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: runner window smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunRunnerWindowSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: event synth smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunEventSynthSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: widget capture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunWidgetCaptureSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: tag store smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunTagStoreSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: tag click smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunTagClickSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: evidence smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunEvidenceSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: save fixture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(() => _ = RunSaveFixtureSmoke(), LoadingEvent, doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: suite passed") {
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
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
      Log.Error(ex.ToString());
    }
  }
}
