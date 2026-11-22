using RimWorks.Pickle.Hosting;
using Verse;

namespace RimWorks.Pickle.RimWorldHost;

internal sealed class GameClock : IGameClock {
  public bool IsGameRunning => Current.Game != null;

  public int Ticks => Find.TickManager.TicksGame;

  public bool IsPaused => Find.TickManager.Paused;

  public bool IsForcePaused {
    get {
#if RW_1_5
      return (Find.WindowStack?.WindowsForcePause ?? false) || LongEventHandler.ForcePause;
#else
      return Find.TickManager.ForcePaused;
#endif
    }
  }

  public void SetNormalSpeed() {
    Find.TickManager.CurTimeSpeed = TimeSpeed.Normal;
  }
}
