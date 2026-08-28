using System.Linq;
using System.Text;
using RimWorld;
using Verse;
using Verse.AI;

namespace Pickle.Vanilla;

/// <summary>
/// Renders what a pawn is actually doing. A failure saying only "actual job: (none)"
/// leaves you guessing whether the pawn is drafted, downed, or dead.
/// </summary>
public static class PawnState {
  public static string Describe(Pawn pawn) {
    StringBuilder state = new StringBuilder();
    state.Append("job=").Append(pawn.CurJobDef?.defName ?? "(none)");

    if (pawn.jobs?.curDriver != null) {
      state.Append(" driver=").Append(pawn.jobs.curDriver.GetType().Name);
    }

    int queued = pawn.jobs?.jobQueue?.Count ?? 0;
    if (queued > 0) {
      state.Append(" queued=").Append(queued);
    }

    state.Append(" drafted=").Append(pawn.drafter?.Drafted ?? false);
    state.Append(" stance=").Append(pawn.stances?.curStance?.GetType().Name ?? "(none)");
    state.Append(" pos=").Append(pawn.Position);

    if (pawn.Dead) {
      state.Append(" dead=true");
    }

    if (pawn.Downed) {
      state.Append(" downed=true");
    }

    if (pawn.InMentalState) {
      state.Append(" mental=").Append(pawn.MentalStateDef?.defName);
    }

    return state.ToString();
  }

  // One line per colonist, for the scenario-wide dump that fires on any failure.
  public static string DescribeColonists(Map map) {
    StringBuilder state = new StringBuilder();
    state.Append("tick=").Append(Find.TickManager?.TicksGame ?? 0);
    state.Append(" paused=").Append(Find.TickManager?.Paused ?? false);
    state.Append(" map=").Append(map.Index).Append('\n');

    foreach (Pawn pawn in map.mapPawns.FreeColonists.OrderBy(p => p.Name?.ToStringShort)) {
      state.Append("  ").Append(pawn.Name?.ToStringShort ?? pawn.LabelCap)
           .Append(": ").Append(Describe(pawn)).Append('\n');
    }

    return state.ToString().TrimEnd();
  }
}
