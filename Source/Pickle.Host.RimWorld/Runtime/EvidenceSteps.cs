using RimWorks.Pickle.Core.Steps;
using Verse;
using Log = RimWorks.RimLogging.Log;

namespace RimWorks.Pickle.Runtime;

/// <summary>A step that always fails, so a run can prove its evidence collection works.</summary>
[PickleSteps]
public class EvidenceSteps {
  /// <summary>Logs an error, attaches a note, then fails, producing evidence to check for.</summary>
  /// <param name="ctx">The running scenario's context.</param>
  [Given("evidence step fails")]
  public void EvidenceStepFails(PickleContext ctx) {
    Log.Error("evidence test error message");
    ctx.Attach("note", "attached-value");
    ctx.Assert(false, "deliberate evidence failure");
  }

  /// <summary>A fixed state dump used to prove <see cref="PickleStateDumpAttribute"/> collection runs.</summary>
  /// <returns>A fixed marker string.</returns>
  [PickleStateDump]
  public string StateDumpForEvidence() {
    return "evidence-dump-ok";
  }
}
