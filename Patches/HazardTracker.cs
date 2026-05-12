namespace StatsTracker.Patches;

internal class HazardTracker
{
  public static int turretCount = 0;
  public static int landmineCount = 0;
  public static int spiketrapCount = 0;

  public static void CountLandmine(Landmine __instance)
  {
    landmineCount++;
  }

  public static void CountTurret(Turret __instance)
  {
    turretCount++;
  }

  public static void CountSpiketrap(SpikeRoofTrap __instance)
  {
    spiketrapCount++;
  }
}
