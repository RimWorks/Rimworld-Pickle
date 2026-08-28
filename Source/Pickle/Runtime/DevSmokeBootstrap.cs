using System;
using UnityEngine.SceneManagement;
using Verse;

namespace Pickle.Runtime;

[StaticConstructorOnStartup]
public static class DevSmokeBootstrap {
  static DevSmokeBootstrap() {
    string? marker = Environment.GetEnvironmentVariable("MARKER");
    if (marker == "pickle: pump smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunQuickTestSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: fixture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunFixtureSmokeTest, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: run session smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunRunSessionSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: runner window smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunRunnerWindowSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: event synth smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunEventSynthSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: widget capture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunWidgetCaptureSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: tag store smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunTagStoreSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: tag click smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunTagClickSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: evidence smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunEvidenceSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: save fixture smoke passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunSaveFixtureSmoke, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }

    if (marker == "pickle: suite passed") {
      PickleDriver.EnsureExists();
      LongEventHandler.QueueLongEvent(RunSuite, "LoadingLongEvent", doAsynchronously: true, exceptionHandler: null);
      return;
    }
  }

  private static async void RunSaveFixtureSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);
      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      SaveFixtureSmoke.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }

  private static async void RunQuickTestSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      PumpSmoke.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }

  private static async void RunFixtureSmokeTest() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      FixtureSmoke.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }

  private static async void RunRunSessionSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      RunSessionSmoke.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }

  private static async void RunRunnerWindowSmoke() {
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

  private static async void RunEventSynthSmoke() {
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

  private static async void RunWidgetCaptureSmoke() {
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

  private static async void RunTagStoreSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      TagStoreSmoke.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }

  private static async void RunTagClickSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      TagClickSmoke.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }

  private static async void RunEvidenceSmoke() {
    try {
      SceneManager.LoadScene(GenScene.PlaySceneName);

      PickleDriver driver = PickleDriver.Instance;
      await driver.WaitUntil(() => Current.ProgramState == ProgramState.Playing, 180f);
      await driver.WaitTicks(5);

      EvidenceSmoke.Run();
    } catch (Exception ex) {
      Log.Error(ex.ToString());
    }
  }

  private static async void RunSuite() {
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
