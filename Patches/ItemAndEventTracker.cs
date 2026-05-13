using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace StatsTracker.Patches;

internal class ItemAndEventTracker
{
  private const int knifeValue = 35;

  private static HashSet<NetworkObjectReference> appSpawnedThisDay = new();
  private static HashSet<NetworkObjectReference> objectsNaturallySpawnedThisDay = new();
  private static HashSet<NetworkObjectReference> objectsExtraSpawnedThisDay = new();
  private static Dictionary<NetworkObjectReference, int> valueFromGiftSpawner = new();
  private static Dictionary<NetworkObjectReference, int> indexFromGiftBox = new();

  public static void ResetTrackerWhenStartingNewDay(RoundManager __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    appSpawnedThisDay.Clear();
    objectsNaturallySpawnedThisDay.Clear();
    objectsExtraSpawnedThisDay.Clear();
    valueFromGiftSpawner.Clear();
    indexFromGiftBox.Clear();
  }

  public static void TrackDungeonInfo(RoundManager __instance, NetworkObjectReference[] spawnedScrap, int[] allScrapValue) 
  {      
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    objectsNaturallySpawnedThisDay = new(spawnedScrap);

    int totalStartScrapValue = 0;
    foreach (int scrapValue in allScrapValue)
      totalStartScrapValue += scrapValue;

    //Reflection needed because this isn't loaded at this point for some reason idk
    string interiorNameIndirect = Traverse.Create(__instance)
      .Field("dungeonGenerator")
      .Field("Generator")
      .Field("DungeonFlow")
      .Property("name")
      .GetValue<string>();

    bool isVanillaInterior = StatsTracker.VanillaInteriorNames.TryGetValue(interiorNameIndirect, out string interiorName);
    StatsTracker.DayStats?.DungeonInfo = new(spawnedScrap.Length + appSpawnedThisDay.Count, isVanillaInterior ? interiorName : interiorNameIndirect);

    StatsTracker.DayStats?.BottomLine += totalStartScrapValue;

    StatsTracker.DayStats?.HazardInfo = new(HazardTracker.turretCount, HazardTracker.landmineCount, HazardTracker.spiketrapCount);
    HazardTracker.turretCount = HazardTracker.landmineCount = HazardTracker.spiketrapCount = 0;
  }

  public static void TrackSID(RoundManager __instance, NetworkObjectReference[] spawnedScrap) 
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
        return;
    }

    StatsTracker.DayStats?.SIDType = first.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText;
  }

  public static void TrackInfestation(RoundManager __instance) 
  {      
    if (__instance.enemyRushIndex != -1)
      StatsTracker.DayStats?.InfestationType = __instance.currentLevel.Enemies[__instance.enemyRushIndex].enemyType.name;
  }

  public static void TrackIndoorFog(RoundManager __instance)
  {
    StatsTracker.DayStats?.IndoorFog = __instance.indoorFog.gameObject.activeSelf;
  }

  public static void TrackMeteorShower(TimeOfDay __instance) 
  { 
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats?.MeteorShowerTime = StatsTracker.GetCurrentTimeString();
  }

  public static void CountApp(LungProp __instance)
  {
    StatsTracker.DayStats?.AppSpawned = true;
    appSpawnedThisDay.Add(__instance.NetworkObject); 
  }

  public static void TrackSpawnedItems(NetworkBehaviour __instance)
  {
    if (__instance is not GrabbableObject || !StatsTracker.dayHasStarted)
      return;

    GrabbableObject gObject = (GrabbableObject)__instance;
    if (!gObject.itemProperties.isScrap || gObject is RagdollGrabbableObject)
      return;

    objectsExtraSpawnedThisDay.Add(gObject.NetworkObject);
    StartOfRound.Instance.StartCoroutine(WaitUntilItemValueHasBeenSetAndUpdateBottomLine(gObject));
  }

  private static IEnumerator WaitUntilItemValueHasBeenSetAndUpdateBottomLine(GrabbableObject instance)
  {
    yield return new WaitUntil(() => instance.scrapValue != 0);

    if (StatsTracker.GiftBoxItemType == null || !StatsTracker.GiftBoxItemType.IsInstanceOfType(instance))
      StatsTracker.DayStats?.BottomLineTrue += instance.scrapValue;
  }

  public static void TrackMissedItems(NetworkBehaviour __instance)
  {
    if (__instance is not GrabbableObject || !StatsTracker.dayHasStarted)
      return;

    GrabbableObject gObject = (GrabbableObject)__instance;
    if (!gObject.itemProperties.isScrap || gObject is RagdollGrabbableObject || (StatsTracker.DeactivatedField != null && (bool)StatsTracker.DeactivatedField.GetValue(gObject)))
      return;

    StatsTracker.DayStats?.MissedItems.Add(new(gObject.gameObject.GetComponentInChildren<ScanNodeProperties>() == null ? gObject.itemProperties.name : gObject.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText, gObject.scrapValue, gObject.transform.position, gObject.scrapPersistedThroughRounds));
  }

  public static void TrackCollectedItems(StartOfRound __instance)
  {
    GrabbableObject[] allObjs = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
    foreach (GrabbableObject gObj in allObjs)
    {
      if (!gObj.scrapPersistedThroughRounds && gObj.itemProperties.isScrap && !(StatsTracker.DeactivatedField != null && (bool)StatsTracker.DeactivatedField.GetValue(gObj)) && !gObj.itemUsedUp && gObj.isInShipRoom)
      {
        StatsTracker.DayStats?.CollectedTotal += 
          StatsTracker.GiftBoxItemType?.IsInstanceOfType(gObj) == true ?
          Traverse.Create(gObj).Field("objectInPresentValue").GetValue<int>() :
          gObj.scrapValue;

        if (objectsNaturallySpawnedThisDay.Contains(gObj.NetworkObject))
          StatsTracker.DayStats?.CollectedNoExtra += gObj.scrapValue;
        else if (valueFromGiftSpawner.TryGetValue(gObj.NetworkObject, out int originalGitfValue))
          StatsTracker.DayStats?.CollectedNoExtra += originalGitfValue;

        if (gObj.itemProperties.name == "RedLocustHive")
          StatsTracker.DayStats?.BeeInfo.AddToCollected(gObj.scrapValue);
        else if (StatsTracker.EggItemType?.IsInstanceOfType(gObj) == true)
          StatsTracker.DayStats?.EggInfo.AddToCollected(gObj.scrapValue);
        else if (StatsTracker.ShotgunItemType?.IsInstanceOfType(gObj) == true)
          StatsTracker.DayStats?.ShotgunInfo.AddToCollected(gObj.scrapValue);
        else if (StatsTracker.KnifeItemType?.IsInstanceOfType(gObj) == true)
          StatsTracker.DayStats?.KnifeInfo.AddToCollected(gObj.scrapValue);

        if (indexFromGiftBox.TryGetValue(gObj.NetworkObject, out int index))
          StatsTracker.DayStats?.GiftBoxes[index].Collected = true;
      }
    }
  }

  public static void TrackTrueValueFromGiftBox(GrabbableObject __instance)
  { 
    if (__instance is not GiftBoxItem)
      return;

    //There's a frame wait for client to get the correct values
    StartOfRound.Instance.StartCoroutine(WaitUntilGiftValuesHaveBeenSetAndUpdateBottomLine((GiftBoxItem)__instance));
  }

  private static IEnumerator WaitUntilGiftValuesHaveBeenSetAndUpdateBottomLine(GiftBoxItem instance)
  {
    yield return new WaitUntil(() => instance.objectInPresentValue != 0 && instance.scrapValue != 0);

    indexFromGiftBox[instance.NetworkObject] = StatsTracker.DayStats!.GiftBoxes.Count;
    StatsTracker.DayStats?.GiftBoxes.Add(new(instance.objectInPresentValue, instance.scrapValue));
    StatsTracker.DayStats?.BottomLineTrue += instance.objectInPresentValue;
  }

  public static void AddNewlySpawnedGiftItemToItemTracker(GiftBoxItem __instance, NetworkObjectReference netObjectRef)
  { 
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    if (!StatsTracker.dayHasStarted || __instance.scrapPersistedThroughRounds)
      return;

    // Using StartOfRound to make sure the coroutine doesn't get interrupted early if the gift instance is destroyed somehow
    StartOfRound.Instance.StartCoroutine(WaitForGiftItemToFullySpawnBeforeTracking(netObjectRef, __instance));
  }

  private static IEnumerator WaitForGiftItemToFullySpawnBeforeTracking(NetworkObjectReference netObjRef, GiftBoxItem instance)
  {
    NetworkObject netObject = null!;

    WaitForSeconds waitTime = new WaitForSeconds(0.03f);
    float startTime = Time.realtimeSinceStartup;
    while (Time.realtimeSinceStartup - startTime < 8f && !netObjRef.TryGet(out netObject))
    {
      yield return waitTime;
    }
    if (netObject == null)
    {
      StatsTracker.Logger.LogWarning("No network object found for giftbox");
      yield break;
    }

    valueFromGiftSpawner[netObjRef] = instance.scrapValue;
    indexFromGiftBox[instance.NetworkObject] = StatsTracker.DayStats!.GiftBoxes.Count;
    StatsTracker.DayStats?.BottomLineTrue -= instance.objectInPresentValue;
  }

  public static void TrackHive(RedLocustBees __instance, int hiveScrapValue)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats?.BeeInfo.AddToAvailable(hiveScrapValue);
    StatsTracker.DayStats?.BottomLineTrue += hiveScrapValue;
  }

  public static void TrackKnifeBeforePopping(ButlerEnemyAI __instance)
  {
    StatsTracker.DayStats?.BottomLineTrue += knifeValue;
  }

  public static void TrackButlerPopAndRemoveFakeValue(ButlerEnemyAI __instance)
  {
    StatsTracker.DayStats?.BottomLineTrue -= knifeValue;
  }

  public static void TrackEggs(GiantKiwiAI __instance,  int[] eggScrapValues) 
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    foreach (int eggValue in eggScrapValues)
    {
      StatsTracker.DayStats?.EggInfo.AddToAvailable(eggValue);
      StatsTracker.DayStats?.BottomLineTrue += eggValue;
    }
  }
}
