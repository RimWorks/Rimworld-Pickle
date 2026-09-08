using System.Threading.Tasks;

namespace RimWorks.Pickle.Runtime;

/// <summary>The simplest possible step, used to prove a scenario can run and assert at all.</summary>
[PickleSteps]
public class EngineSmokeSteps {
  /// <summary>Always passes. There is nothing to check beyond the step running.</summary>
  /// <param name="ctx">The scenario's context.</param>
  [Then("the engine is alive")]
  public void EngineIsAlive(PickleContext ctx) {
    ctx.Assert(true, "engine is alive");
  }
}
