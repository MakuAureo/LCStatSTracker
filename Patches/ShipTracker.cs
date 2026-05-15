namespace StatsTracker.Patches;

internal class ShipTracker
{
  public static void RegisterTakeOffTime(StartOfRound __instance)
  {
    StatsTracker.DayStats!.TakeOffTime = StatsTracker.GetCurrentTimeString();
  }
}
