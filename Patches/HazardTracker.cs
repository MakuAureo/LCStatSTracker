using System;
using System.Runtime.CompilerServices;
using HarmonyLib;

namespace StatsTracker.Patches;

internal class HazardTracker
{
  public static int turretCount = 0;
  public static int landmineCount = 0;
  public static int spiketrapCount = 0;

  public static void ApplyHazardTrakcerPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(Landmine), nameof(Landmine.Start)), postfix: new HarmonyMethod(typeof(Patches.HazardTracker), nameof(Patches.HazardTracker.CountLandmine)));
    Harmony.Patch(AccessTools.Method(typeof(Turret), nameof(Turret.Start)), postfix: new HarmonyMethod(typeof(Patches.HazardTracker), nameof(Patches.HazardTracker.CountTurret)));
    Type? SpikeRoofTrapType = AccessTools.TypeByName(nameof(SpikeRoofTrap));
    if (SpikeRoofTrapType != null)
      SpikeRoofTrapTypePath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void SpikeRoofTrapTypePath() => Harmony.Patch(AccessTools.Method(SpikeRoofTrapType, nameof(SpikeRoofTrap.Start)), postfix: new HarmonyMethod(typeof(Patches.HazardTracker), nameof(Patches.HazardTracker.CountSpiketrap)));
  }

  private static void CountLandmine(Landmine __instance)
  {
    landmineCount++;
  }

  private static void CountTurret(Turret __instance)
  {
    turretCount++;
  }

  private static void CountSpiketrap(SpikeRoofTrap __instance)
  {
    spiketrapCount++;
  }
}
