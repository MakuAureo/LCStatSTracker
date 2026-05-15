using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Logging;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
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

    //Company
    Harmony.Patch(AccessTools.Method(typeof(DepositItemsDesk), nameof(DepositItemsDesk.SellItemsOnServer)), prefix: new HarmonyMethod(typeof(Patches.CompanyTracker), nameof(Patches.CompanyTracker.CalculateAmountSold)));

    //Hazard
    Harmony.Patch(AccessTools.Method(typeof(Landmine), nameof(Landmine.Start)), postfix: new HarmonyMethod(typeof(Patches.HazardTracker), nameof(Patches.HazardTracker.CountLandmine)));
    Harmony.Patch(AccessTools.Method(typeof(Turret), nameof(Turret.Start)), postfix: new HarmonyMethod(typeof(Patches.HazardTracker), nameof(Patches.HazardTracker.CountTurret)));
    Type? SpikeRoofTrapType = AccessTools.TypeByName(nameof(SpikeRoofTrap));
    if (SpikeRoofTrapType != null)
      Harmony.Patch(AccessTools.Method(SpikeRoofTrapType, nameof(SpikeRoofTrap.Start)), postfix: new HarmonyMethod(typeof(Patches.HazardTracker), nameof(Patches.HazardTracker.CountSpiketrap)));

    //ItemAndEvent
    Harmony.Patch(AccessTools.Method(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc)), prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.ResetTrackerWhenStartingNewDay)));

    MethodInfo RoundManagerSyncScrapValuesClientRpc = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc));
    Harmony.Patch(RoundManagerSyncScrapValuesClientRpc, prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackDungeonInfo)));
    Harmony.Patch(RoundManagerSyncScrapValuesClientRpc, prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackSID)));

    MethodInfo RoundManagerRefreshEnemiesList = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.RefreshEnemiesList));
    FieldInfo? enemyRushIndexField = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.enemyRushIndex));
    if (enemyRushIndexField != null)
      Harmony.Patch(RoundManagerRefreshEnemiesList, postfix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackInfestation)));

    FieldInfo? indoorFogInfo = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.indoorFog));
    if (indoorFogInfo != null)
      Harmony.Patch(RoundManagerRefreshEnemiesList, postfix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackIndoorFog)));

    MethodInfo? TimeOfDaySetBeginMeteorShowerClientRpcInfo = AccessTools.Method(typeof(TimeOfDay), nameof(TimeOfDay.SetBeginMeteorShowerClientRpc));
    if (TimeOfDaySetBeginMeteorShowerClientRpcInfo != null)
      Harmony.Patch(TimeOfDaySetBeginMeteorShowerClientRpcInfo, prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackMeteorShower)));

    Harmony.Patch(AccessTools.Method(typeof(LungProp), nameof(LungProp.Start)), postfix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.CountApp)));
    Harmony.Patch(AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn)), postfix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackSpawnedItems)));
    Harmony.Patch(AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkDespawn)), prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackMissedItems)));
    Harmony.Patch(AccessTools.Method(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound)), prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackCollectedItems)));
  
    if (GiftBoxItemType != null)
      Harmony.Patch(AccessTools.Method(GiftBoxItemType, nameof(GiftBoxItem.OpenGiftBoxClientRpc)), prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.AddNewlySpawnedGiftItemToItemTracker)));

    Harmony.Patch(AccessTools.Method(typeof(RedLocustBees), nameof(RedLocustBees.SpawnHiveClientRpc)), prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackHive)));

    Type? ButlerEnemyAIType = AccessTools.TypeByName(nameof(ButlerEnemyAI));
    if (ButlerEnemyAIType != null)
    {
      Harmony.Patch(AccessTools.Method(ButlerEnemyAIType, nameof(ButlerEnemyAI.Start)), postfix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackKnifeBeforePopping)));
      Harmony.Patch(AccessTools.Method(ButlerEnemyAIType, nameof(ButlerEnemyAI.KillEnemy)), postfix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackButlerPopAndRemoveFakeValue)));
    }

    Type? GiantKiwiAIType = AccessTools.TypeByName(nameof(GiantKiwiAI));
    if (GiantKiwiAIType != null)
      Harmony.Patch(AccessTools.Method(GiantKiwiAIType, nameof(GiantKiwiAI.SpawnEggsClientRpc)), prefix: new HarmonyMethod(typeof(Patches.ItemAndEventTracker), nameof(Patches.ItemAndEventTracker.TrackEggs)));

    //Player
    Harmony.Patch(AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerClientRpc)), prefix: new HarmonyMethod(typeof(Patches.PlayerTracker), nameof(Patches.PlayerTracker.TrackDeath)));
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC)), prefix: new HarmonyMethod(typeof(Patches.PlayerTracker), nameof(Patches.PlayerTracker.TrackDisconnect)));

    //Server
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.ResetPlayersLoadedValueClientRpc)), prefix: new HarmonyMethod(typeof(Patches.ServerEvents), nameof(Patches.ServerEvents.StartTrackingNewday)));
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.PassTimeToNextDay)), postfix: new HarmonyMethod(typeof(Patches.ServerEvents), nameof(Patches.ServerEvents.PublishDayStats)));

    //Ship
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.ShipLeave)), postfix: new HarmonyMethod(typeof(Patches.ShipTracker), nameof(Patches.ShipTracker.RegisterTakeOffTime)));

    //Spawn
    Harmony.Patch(AccessTools.Method(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc)), prefix: new HarmonyMethod(typeof(Patches.SpawnTracker), nameof(Patches.SpawnTracker.ResetTrackerWhenStartingNewDay)));
    Harmony.Patch(AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.Start)), postfix: new HarmonyMethod(typeof(Patches.SpawnTracker), nameof(Patches.SpawnTracker.TrackSpawn)));
    Harmony.Patch(AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemy)), prefix: new HarmonyMethod(typeof(Patches.SpawnTracker), nameof(Patches.SpawnTracker.TrackDeath)));

    //HQoL
    if (Chainloader.PluginInfos.ContainsKey("OreoM.HQoL.72") || Chainloader.PluginInfos.ContainsKey("OreoM.HQoL.73"))
    {
      Harmony.Patch(AccessTools.Method(typeof(DepositItemsDesk), nameof(DepositItemsDesk.Start)), postfix: new HarmonyMethod(typeof(Patches.HQoLTracker), nameof(Patches.HQoLTracker.RegisterOnChangeWhenLandingOnCompanyTypeMoon)));
      Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft)), prefix: new HarmonyMethod(typeof(Patches.HQoLTracker), nameof(Patches.HQoLTracker.DeregisterOnChangeAfterTakingOffCompanyTypeMoon)));
    }

    Logger.LogDebug("Finished patching!");
  }

  internal static void Unpatch()
  {
    Logger.LogDebug("Unpatching...");

    Harmony?.UnpatchSelf();

    Logger.LogDebug("Finished unpatching!");
  }

  private static HarmonyMethod PatchMethod(Type type, MethodInfo method)
  {
    return new HarmonyMethod(type, nameof(method));
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
