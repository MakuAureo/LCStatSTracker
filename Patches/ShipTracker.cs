using HarmonyLib;

namespace StatsTracker.Patches;

internal class ShipTracker
{
  public static void ApplyShipTrackerPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.ShipLeave)), postfix: new HarmonyMethod(typeof(ShipTracker), nameof(RegisterTakeOffTime)));
  }

  private static void RegisterTakeOffTime(StartOfRound __instance)
  {
    StatsTracker.DayStats!.TakeOffTime = StatsTracker.GetCurrentTimeString();
  }
}
