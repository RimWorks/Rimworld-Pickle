using Pickle.Core.Steps;
using Verse;

namespace Pickle.Runtime;

[PickleSteps]
public class EvidenceSteps {
  [Given("evidence step fails")]
  public void EvidenceStepFails(PickleContext ctx) {
    Log.Error("evidence test error message");
    ctx.Attach("note", "attached-value");
    ctx.Assert(false, "deliberate evidence failure");
  }

  [PickleStateDump]
  public string StateDumpForEvidence() {
    return "evidence-dump-ok";
  }
}
