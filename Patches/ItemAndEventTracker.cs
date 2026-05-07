using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Rendering.HighDefinition;

namespace StatsTracker.Patches;

[HarmonyPatch]
internal class ItemAndEventTracker
{
  private static readonly Type? GiftBoxItemType = AccessTools.TypeByName(nameof(GiftBoxItem));
  private static readonly Type? VehicleControllerType = AccessTools.TypeByName(nameof(VehicleController));
  private static readonly FieldInfo? DeactivatedField = AccessTools.Field(typeof(GrabbableObject), nameof(GrabbableObject.deactivated));

  private static HashSet<NetworkObjectReference> knivesSpawnedThisDay = new();
  private static HashSet<NetworkObjectReference> shotgunsSpawnedThisDay = new();
  private static HashSet<NetworkObjectReference> hivesSpawnedThisDay = new();
  private static HashSet<NetworkObjectReference> eggsSpawnedThisDay = new();
  private static HashSet<NetworkObjectReference> appSpawnedThisDay = new();
  private static HashSet<NetworkObjectReference> objectsNaturallySpawnedThisDay = new();
  private static Dictionary<NetworkObjectReference, int> valueFromGiftSpawner = new();
  private static HashSet<Vector3> butlerPopPositionsToTrack = new();
  private static List<Util.MissingItemInfo> missedItemsParentedToDungeon = new();

  [HarmonyPatch(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc))]
  [HarmonyPrefix]
  private static void ResetTrackerWhenStartingNewDay(RoundManager __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    appSpawnedThisDay.Clear();
    knivesSpawnedThisDay.Clear();
    shotgunsSpawnedThisDay.Clear();
    hivesSpawnedThisDay.Clear();
    eggsSpawnedThisDay.Clear();
    objectsNaturallySpawnedThisDay.Clear();
    valueFromGiftSpawner.Clear();
    butlerPopPositionsToTrack.Clear();
    missedItemsParentedToDungeon.Clear();
  }

  [HarmonyPatch]
  private static class TrackSpawnedItemsAndHazards
  {
    private static bool Prepare() => true;
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc));
    private static void Prefix(RoundManager __instance, NetworkObjectReference[] spawnedScrap, int[] allScrapValue) 
    {      
      if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
        return;

      objectsNaturallySpawnedThisDay = new(spawnedScrap);

      int totalStartScrapValue = 0;
      foreach (int scrapValue in allScrapValue)
        totalStartScrapValue += scrapValue;

      string interiorNameIndirect = Traverse.Create(__instance)
        .Field("dungeonGenerator")
        .Field("Generator")
        .Field("DungeonFlow")
        .Property("name")
        .GetValue<string>();

      bool isVanillaInterior = StatsTracker.VanillaInteriorNames.TryGetValue(interiorNameIndirect, out string interiorName);
      StatsTracker.DayStats?.DungeonInfo = new(spawnedScrap.Length + (appSpawnedThisDay.Count > 0 ? 1 : 0), isVanillaInterior ? interiorName : interiorNameIndirect);
      StatsTracker.DayStats?.AppSpawned = appSpawnedThisDay.Count > 0;

      StatsTracker.DayStats?.BottomLine += totalStartScrapValue;
      StatsTracker.DayStats?.BottomLineTrue += totalStartScrapValue;

      StatsTracker.DayStats?.HazardInfo = new(HazardTracker.turretCount, HazardTracker.landmineCount, HazardTracker.spiketrapCount);
      HazardTracker.turretCount = HazardTracker.landmineCount = HazardTracker.spiketrapCount = 0;
    }
  }

  [HarmonyPatch]
  private static class TrackSID
  {
    private static bool Prepare() => true;
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc));
    private static void Prefix(RoundManager __instance, NetworkObjectReference[] spawnedScrap) 
    {      
      if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client) || GameNetworkManager.Instance.gameVersionNum < 60)
        return;

      spawnedScrap[0].TryGet(out var firstNetObj);
      GrabbableObject first = firstNetObj.GetComponent<GrabbableObject>();
      if (first == null) 
      {
        StatsTracker.Logger.LogWarning("Unable to retrieve first GrabbableObject from the spawned objects");
        return;
      }

      foreach (NetworkObjectReference netObjRef in spawnedScrap)
      {
        netObjRef.TryGet(out var netObj);
        GrabbableObject component = netObj.GetComponent<GrabbableObject>();
        if (component == null)
        {
          StatsTracker.Logger.LogWarning("Unable to retrieve some GrabbableObject from the spawned objects");
          return;
        }

        if (component.itemProperties.name != first.itemProperties.name)
        {
          return;
        }
      }

      StatsTracker.DayStats?.SIDType = first.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText;
    }
  }

  [HarmonyPatch]
  private static class TrackInfes
  {
    private static FieldInfo? enemyRushIndexInfo = null;
    private static bool Prepare()
    { 
      enemyRushIndexInfo = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.enemyRushIndex));
      return enemyRushIndexInfo != null;
    }
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc));
    private static void Postfix(RoundManager __instance) 
    {      
      int enemyRushIndex = (int)enemyRushIndexInfo!.GetValue(__instance);
      if (enemyRushIndex != -1)
        StatsTracker.DayStats?.InfestationType = __instance.currentLevel.Enemies[enemyRushIndex].enemyType.name;
    }
  }

  [HarmonyPatch]
  private static class TrackIndoorFog
  {
    private static FieldInfo? indoorFogInfo = null;
    private static bool Prepare()
    {
      indoorFogInfo = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.indoorFog));
      return indoorFogInfo != null;
    }
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc));
    private static void Postfix(RoundManager __instance) { StatsTracker.DayStats?.IndoorFog = ((LocalVolumetricFog)indoorFogInfo!.GetValue(__instance)).gameObject.activeSelf; }
  }

  [HarmonyPatch]
  private static class TrackMeteorShower
  {
    private static MethodInfo? SetBeginMeteorShowerClientRpcInfo = null;
    private static bool Prepare()
    {
      SetBeginMeteorShowerClientRpcInfo = AccessTools.Method(typeof(TimeOfDay), nameof(TimeOfDay.SetBeginMeteorShowerClientRpc));
      return SetBeginMeteorShowerClientRpcInfo != null;
    }
    private static MethodBase TargetMethod() => SetBeginMeteorShowerClientRpcInfo!;
    private static void Prefix(TimeOfDay __instance) 
    { 
      if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
        return;

      StatsTracker.DayStats?.MeteorShowerTime = StatsTracker.GetCurrentTimeString();
    }
  }

  [HarmonyPatch(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkDespawn))]
  [HarmonyPostfix]
  private static void TrackMissedItems(NetworkBehaviour __instance)
  {
    if (__instance is not GrabbableObject || StartOfRound.Instance.inShipPhase)
      return;

    GrabbableObject gObject = (GrabbableObject)__instance;
    if (!gObject.itemProperties.isScrap || gObject is RagdollGrabbableObject || (DeactivatedField != null && (bool)DeactivatedField.GetValue(gObject)))
      return;

    StatsTracker.DayStats?.MissedItems.Add(new(gObject.gameObject.GetComponentInChildren<ScanNodeProperties>() == null ? gObject.itemProperties.name : gObject.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText, gObject.scrapValue, gObject.transform.position, gObject.scrapPersistedThroughRounds));
  }

  [HarmonyPatch(typeof(LungProp), nameof(LungProp.Start))]
  [HarmonyPostfix]
  private static void CountApp(LungProp __instance)
  {
    appSpawnedThisDay.Add(__instance.NetworkObject); 
    StatsTracker.DayStats?.BottomLineTrue += __instance.scrapValue;
  }

  [HarmonyPatch(typeof(RedLocustBees), nameof(RedLocustBees.SpawnHiveClientRpc))]
  [HarmonyPrefix]
  private static void TrackHiveItem(RedLocustBees __instance, NetworkObjectReference hiveObject)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    hivesSpawnedThisDay.Add(hiveObject);
  }

  [HarmonyPatch]
  private static class TrackEggItems
  {
    private static Type? GiantKiwiAIType = null;
    private static bool Prepare()
    {
      GiantKiwiAIType = AccessTools.TypeByName(nameof(GiantKiwiAI));
      return GiantKiwiAIType != null;
    }
    private static MethodBase TargetMethod() => AccessTools.Method(GiantKiwiAIType, nameof(GiantKiwiAI.SpawnEggsClientRpc));
    private static void Prefix(object __instance, NetworkObjectReference[] eggNetworkReferences)
    { 
      GiantKiwiAI instance = (GiantKiwiAI)__instance;     

      if ((GameNetworkManager.Instance.gameVersionNum > 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
        return;

      foreach (NetworkObjectReference eggNetRef in eggNetworkReferences)
        eggsSpawnedThisDay.Add(eggNetRef);
    }
  }

  [HarmonyPatch]
  private static class TrackTrueValueFromGiftBox
  {
    private static bool Prepare() => GiftBoxItemType != null;
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.Start));
    private static void Postfix(object __instance)
    { 
      if (!(GiftBoxItemType!.IsInstanceOfType(__instance)))
        return;

      GiftBoxItem instance = (GiftBoxItem)__instance;     
      StatsTracker.DayStats?.BottomLineTrue += instance.objectInPresentValue - instance.scrapValue;
    }
  }

  [HarmonyPatch]
  private static class TrackShotgun
  {
    private static Type? NutcrackerEnemyAIType = null;
    private static bool Prepare()
    {
      NutcrackerEnemyAIType = AccessTools.TypeByName(nameof(NutcrackerEnemyAI));
      return NutcrackerEnemyAIType != null;
    }
    private static MethodBase TargetMethod() => AccessTools.Method(NutcrackerEnemyAIType, nameof(NutcrackerEnemyAI.InitializeNutcrackerValuesClientRpc));
    private static void Prefix(object __instance, NetworkObjectReference gunObject)
    {
      NutcrackerEnemyAI instance = (NutcrackerEnemyAI)__instance;

      if ((GameNetworkManager.Instance.gameVersionNum > 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
        return;

      shotgunsSpawnedThisDay.Add(gunObject);
    }
  }

  [HarmonyPatch]
  private static class TrackButlerPopPosition
  {
    private static Type? ButlerEnemyAIType = null;
    private static bool Prepare()
    {
      ButlerEnemyAIType = AccessTools.TypeByName(nameof(ButlerEnemyAI));
      return ButlerEnemyAIType != null;
    }
    private static MethodBase TargetMethod() => AccessTools.Method(ButlerEnemyAIType, nameof(ButlerEnemyAI.KillEnemy));
    private static void Prefix(object __instance)
    {
      ButlerEnemyAI instance = (ButlerEnemyAI)__instance;

      butlerPopPositionsToTrack.Add(instance.transform.position);
    }
  }

  [HarmonyPatch]
  private static class TrackKnife
  {
    private static Type? KnifeItemInfo = null;
    private static bool Prepare()
    {
      KnifeItemInfo = AccessTools.TypeByName(nameof(KnifeItem));
      return KnifeItemInfo != null;
    }
    private static MethodBase TargetMethod() => AccessTools.Method(typeof(GrabbableObject), nameof(GrabbableObject.Start));
    private static void Prefix(GrabbableObject __instance)
    {
      if (!(KnifeItemInfo!.IsInstanceOfType(__instance)))
        return;

      KnifeItem knife = (KnifeItem)__instance;

      Vector3 pos = butlerPopPositionsToTrack.FirstOrDefault(p => Vector3.Distance(p + Vector3.up * 0.5f, knife.transform.position) < 0.1f);
      if (pos != Vector3.zero)
      {
        knivesSpawnedThisDay.Add(__instance.NetworkObject);
        butlerPopPositionsToTrack.Remove(pos);
      }
    }
  }

  [HarmonyPatch(typeof(PlayerControllerB), nameof(PlayerControllerB.SetItemInElevator))]
  [HarmonyPrefix]
  private static void TrackItemWhenDropped(PlayerControllerB __instance, bool droppedInShipRoom, GrabbableObject gObject)
  {
    if (!gObject.IsSpawned || StartOfRound.Instance.inShipPhase || gObject.isInShipRoom == droppedInShipRoom || gObject.scrapPersistedThroughRounds)
      return;

    if (droppedInShipRoom)
    {
      if (objectsNaturallySpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedNoExtra += gObject.scrapValue;
        StatsTracker.DayStats?.CollectedTotal += 
          GiftBoxItemType?.IsInstanceOfType(gObject) == true ?
          Traverse.Create(gObject).Property(nameof(GiftBoxItem.objectInPresentValue)).GetValue<int>()
          : gObject.scrapValue;
      }
      else if (valueFromGiftSpawner.TryGetValue(gObject.NetworkObject, out int parentGiftValue))
      {
        StatsTracker.DayStats?.CollectedNoExtra += parentGiftValue;
        StatsTracker.DayStats?.CollectedTotal += gObject.scrapValue;
      }
      else if (shotgunsSpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedTotal += gObject.scrapValue;
        StatsTracker.DayStats?.ShotgunsCollected += 1;
      }
      else if (knivesSpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedTotal += gObject.scrapValue;
        StatsTracker.DayStats?.KnivesCollected += 1;
      }
      else if (hivesSpawnedThisDay.Contains(gObject.NetworkObject) || eggsSpawnedThisDay.Contains(gObject.NetworkObject) || appSpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedTotal += gObject.scrapValue;
      }
    }
    else
    {
      if (objectsNaturallySpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedNoExtra -= gObject.scrapValue;
        StatsTracker.DayStats?.CollectedTotal -= 
          GiftBoxItemType?.IsInstanceOfType(gObject) == true ?
          Traverse.Create(gObject).Property(nameof(GiftBoxItem.objectInPresentValue)).GetValue<int>()
          : gObject.scrapValue;
      }
      else if (valueFromGiftSpawner.TryGetValue(gObject.NetworkObject, out int parentGiftValue))
      {
        StatsTracker.DayStats?.CollectedNoExtra -= parentGiftValue;
        StatsTracker.DayStats?.CollectedTotal -= gObject.scrapValue;
      }
      else if (shotgunsSpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedTotal -= gObject.scrapValue;
        StatsTracker.DayStats?.ShotgunsCollected -= 1;
      }
      else if (knivesSpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedTotal -= gObject.scrapValue;
        StatsTracker.DayStats?.KnivesCollected -= 1;
      }
      else if (hivesSpawnedThisDay.Contains(gObject.NetworkObject) || eggsSpawnedThisDay.Contains(gObject.NetworkObject) || appSpawnedThisDay.Contains(gObject.NetworkObject))
      {
        StatsTracker.DayStats?.CollectedTotal -= gObject.scrapValue;
      }
    }
  }

  [HarmonyPatch]
  private static class AddNewlySpawnedGiftItemToItemTracker
  {
    private static bool Prepare() => GiftBoxItemType != null;
    private static MethodBase TargetMethod() => AccessTools.Method(GiftBoxItemType, nameof(GiftBoxItem.OpenGiftBoxClientRpc));
    private static void Prefix(object __instance, NetworkObjectReference netObjectRef)
    { 
      GiftBoxItem instance = (GiftBoxItem)__instance;     
      if ((GameNetworkManager.Instance.gameVersionNum > 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
        return;

      if (StartOfRound.Instance.inShipPhase)
        return;

      // Using StartOfRound to make sure the coroutine doesn't get interrupted early if the gift instance is destroyed somehow
      StartOfRound.Instance.StartCoroutine(WaitForGiftItemToFullySpawnBeforeTracking(netObjectRef, instance.scrapValue));
    }
  }

  private static IEnumerator WaitForGiftItemToFullySpawnBeforeTracking(NetworkObjectReference netObjRef, int giftScrapValue)
  {
    NetworkObject netObject = null!;
    float startTime = Time.realtimeSinceStartup;
    while (Time.realtimeSinceStartup - startTime < 8f && !netObjRef.TryGet(out netObject))
    {
      yield return new WaitForSeconds(0.03f);
    }
    if (netObject == null)
    {
      StatsTracker.Logger.LogWarning("No network object found for giftbox");
      yield break;
    }

    // Make sure the items were already set to Elevator before tracking (this isn't guaranteed to wait for long enough, but like yk)
    yield return new WaitForSeconds(0.3f);

    valueFromGiftSpawner[netObjRef] = giftScrapValue;
  }
}
