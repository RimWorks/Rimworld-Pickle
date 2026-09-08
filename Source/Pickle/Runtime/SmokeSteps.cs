using RimWorks.Pickle.Core.Steps;

namespace RimWorks.Pickle.Runtime;

/// <summary>Fixed pass/fail steps used to smoke test the runner itself, not a mod under test.</summary>
[PickleSteps]
public class SmokeSteps {
  /// <summary>A step that always passes.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [Given("smoke step passes")]
  public void SmokeStepPasses(PickleContext ctx) {
    ctx.Assert(true, "smoke pass");
  }

  /// <summary>A step that always fails, to prove failure reporting works.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [Given("smoke step fails")]
  public void SmokeStepFails(PickleContext ctx) {
    ctx.Assert(1 == 2, "deliberate smoke failure");
  }

  /// <summary>Attaches a fixed piece of evidence before each scenario runs.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [BeforeScenario]
  public void BeforeScenario(PickleContext ctx) {
    ctx.Attach("smoke-attachment", "smoke test attachment content");
  }
}
