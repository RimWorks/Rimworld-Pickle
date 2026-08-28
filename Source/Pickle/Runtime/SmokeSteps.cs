using Pickle.Core.Steps;

namespace Pickle.Runtime;

[PickleSteps]
public class SmokeSteps {
  [Given("smoke step passes")]
  public void SmokeStepPasses(PickleContext ctx) {
    ctx.Assert(true, "smoke pass");
  }

  [Given("smoke step fails")]
  public void SmokeStepFails(PickleContext ctx) {
    ctx.Assert(1 == 2, "deliberate smoke failure");
  }

  [BeforeScenario]
  public void BeforeScenario(PickleContext ctx) {
    ctx.Attach("smoke-attachment", "smoke test attachment content");
  }
}
