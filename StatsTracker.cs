using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using HarmonyLib;
using UnityEngine;

namespace StatsTracker;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("OreoM.HQoL.72", BepInDependency.DependencyFlags.SoftDependency)]
[BepInDependency("OreoM.HQoL.73", BepInDependency.DependencyFlags.SoftDependency)]
public class StatsTracker : BaseUnityPlugin
{
  public static StatsTracker Instance { get; private set; } = null!;
  internal new static ManualLogSource Logger { get; private set; } = null!;
  internal static Harmony? Harmony { get; set; }

  internal static bool dayHasStarted = false;
  internal static Util.Stats? DayStats;
  internal static Util.HttpSSE LocalServer = new();
  internal static Dictionary<string, string> VanillaInteriorNames = new Dictionary<string,string>
  { 
    {"Level1Flow", "Facility"},
    {"Level2Flow", "Mansion"},
    {"Level1FlowExtraLarge", "UnusedFacility"},
    {"Level1Flow3Exits", "Facility"},
    {"Level3Flow", "Mineshaft"} 
  };

  public static readonly Type? EggItemType = AccessTools.TypeByName(nameof(KiwiBabyItem));
  public static readonly Type? KnifeItemType = AccessTools.TypeByName(nameof(KnifeItem));
  public static readonly Type? ShotgunItemType = AccessTools.TypeByName(nameof(ShotgunItem));
  public static readonly Type? GiftBoxItemType = AccessTools.TypeByName(nameof(GiftBoxItem));
  public static readonly FieldInfo? DeactivatedField = AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.deactivated));

  private void Awake()
  {
    Logger = base.Logger;
    Instance = this;

    Patch();

    LocalServer.Start();

    Logger.LogInfo($"{MyPluginInfo.PLUGIN_GUID} v{MyPluginInfo.PLUGIN_VERSION} has loaded!");
  }

  internal static void Patch()
  {
    Harmony ??= new Harmony(MyPluginInfo.PLUGIN_GUID);

    Logger.LogDebug("Patching...");

    Patches.CompanyTracker.ApplyCompanyTrackerPatches(Harmony);
    Patches.HazardTracker.ApplyHazardTrakcerPatches(Harmony);
    Patches.ItemAndEventTracker.ApplyItemAndEventTrackerPatches(Harmony);
    Patches.PlayerTracker.ApplyPlayerTrackerPatches(Harmony);
    Patches.ServerEvents.ApplyServerEventPatches(Harmony);
    Patches.ShipTracker.ApplyShipTrackerPatches(Harmony);
    Patches.SpawnTracker.ApplySpawnTrackerPatches(Harmony);

    if (Chainloader.PluginInfos.ContainsKey("OreoM.HQoL.72") || Chainloader.PluginInfos.ContainsKey("OreoM.HQoL.73"))
      Patches.HQoLTracker.ApplyHQoLTrackerPatches(Harmony);

    Logger.LogDebug("Finished patching!");
  }

  internal static void Unpatch()
  {
    Logger.LogDebug("Unpatching...");

    Harmony?.UnpatchSelf();

    Logger.LogDebug("Finished unpatching!");
  }

  internal static string GetCurrentTimeString()
  {
    float timeNormalized = TimeOfDay.Instance.normalizedTimeOfDay;
    float numberOfHours = TimeOfDay.Instance.numberOfHours;
    bool createNewLine = false;
    string newLine = "";
    string amPM = "";

    int num = (int)(timeNormalized * (60f * numberOfHours)) + 360;
		int num2 = (int)Mathf.Floor(num / 60);
		if (!createNewLine)
		{
			newLine = " ";
		}
		else
		{
			newLine = "\n";
		}
		amPM = newLine + "AM";
		if (num2 >= 24)
		{
			return "12:00 " + newLine + " AM";
		}
		if (num2 < 12)
		{
			amPM = newLine + "AM";
		}
		else
		{
			amPM = newLine + "PM";
		}
		if (num2 > 12)
		{
			num2 %= 12;
		}
		int num3 = num % 60;
    string time = $"{num2:00}:{num3:00}".TrimStart('0') + amPM;
    return (time == "6:00 AM") ? "7:40 AM" : time;
  }
}
