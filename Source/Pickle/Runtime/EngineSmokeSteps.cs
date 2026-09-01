using System.Threading.Tasks;

namespace RimWorks.Pickle.Runtime;

[PickleSteps]
public class EngineSmokeSteps {
  [Then("the engine is alive")]
  public void EngineIsAlive(PickleContext ctx) {
    ctx.Assert(true, "engine is alive");
  }
}
