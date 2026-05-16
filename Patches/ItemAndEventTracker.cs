using System.Collections;
using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;
using UnityEngine;

namespace StatsTracker.Patches;

internal class ItemAndEventTracker
{
  private const int knifeValue = 35;

  private static readonly HashSet<NetworkObjectReference> appSpawnedThisDay = [];
  private static readonly HashSet<NetworkObjectReference> objectsNaturallySpawnedThisDay = [];
  private static readonly HashSet<NetworkObjectReference> objectsExtraSpawnedThisDay = [];
  private static readonly Dictionary<NetworkObjectReference, int> valueFromGiftSpawner = [];
  private static readonly Dictionary<NetworkObjectReference, int> indexFromGiftBox = [];

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

    objectsNaturallySpawnedThisDay.UnionWith(spawnedScrap);

    int totalStartScrapValue = 0;
    foreach (int scrapValue in allScrapValue) {
      totalStartScrapValue += scrapValue;
    }

    //Reflection needed because this isn't loaded at this point for some reason idk
    string interiorNameIndirect = Traverse.Create(__instance)
      .Field(nameof(RoundManager.dungeonGenerator))
      .Field(nameof(DunGen.RuntimeDungeon.Generator))
      .Field(nameof(DunGen.DungeonGenerator.DungeonFlow))
      .Property(nameof(DunGen.Graph.DungeonFlow.name))
      .GetValue<string>();

    bool isVanillaInterior = StatsTracker.VanillaInteriorNames.TryGetValue(interiorNameIndirect, out string interiorName);
    StatsTracker.DayStats!.DungeonInfo = new(spawnedScrap.Length + appSpawnedThisDay.Count, isVanillaInterior ? interiorName : interiorNameIndirect);
    StatsTracker.DayStats!.BottomLine += totalStartScrapValue;

    StatsTracker.DayStats!.HazardInfo = new(HazardTracker.turretCount, HazardTracker.landmineCount, HazardTracker.spiketrapCount);
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

    StatsTracker.DayStats!.SIDType = first.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText;
  }

  public static void TrackInfestation(RoundManager __instance)
  {
    if (__instance.enemyRushIndex != -1)
      StatsTracker.DayStats!.InfestationType = __instance.currentLevel.Enemies[__instance.enemyRushIndex].enemyType.name;
  }

  public static void TrackIndoorFog(RoundManager __instance)
  {
    StatsTracker.DayStats!.IndoorFog = __instance.indoorFog.gameObject.activeSelf;
  }

  public static void TrackMeteorShower(TimeOfDay __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats!.MeteorShowerTime = StatsTracker.GetCurrentTimeString();
  }

  public static void CountApp(LungProp __instance)
  {
    StatsTracker.DayStats!.AppSpawned = true;
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

    StatsTracker.DayStats!.BottomLineTrue += instance.scrapValue;

    if (StatsTracker.GiftBoxItemType?.IsInstanceOfType(instance) == true)
    {
      System.Random randomSeed = new System.Random((int)instance.targetFloorPosition.x + (int)instance.targetFloorPosition.y);
      System.Random random = new System.Random((int)instance.targetFloorPosition.x + (int)instance.targetFloorPosition.y);

      List<int> list = new List<int>(RoundManager.Instance.currentLevel.spawnableScrap.Count);
      for (int i = 0; i < RoundManager.Instance.currentLevel.spawnableScrap.Count; i++)
      {
        if (RoundManager.Instance.currentLevel.spawnableScrap[i].spawnableItem.itemId == 152767) // I think this is the itemId of the giftbox but idk it's just like this in the code
        {
          list.Add(0);
        }
        else
        {
          list.Add(RoundManager.Instance.currentLevel.spawnableScrap[i].rarity);
        }
      }
      int randomWeightedIndexList = RoundManager.Instance.GetRandomWeightedIndexList(list, randomSeed);
      Item itemInGift = RoundManager.Instance.currentLevel.spawnableScrap[randomWeightedIndexList].spawnableItem;
      int itemInGiftValue = (int)((float)random.Next(itemInGift.minValue + 25, itemInGift.maxValue + 35) * RoundManager.Instance.scrapValueMultiplier);
      Traverse.Create(instance).Field(nameof(GiftBoxItem.objectInPresentValue)).SetValue(itemInGiftValue);
    }
  }

  public static void TrackMissedItems(NetworkBehaviour __instance)
  {
    if (__instance is not GrabbableObject || !StatsTracker.dayHasStarted)
      return;

    GrabbableObject gObject = (GrabbableObject)__instance;
    if (!gObject.itemProperties.isScrap || gObject is RagdollGrabbableObject || (bool?)(StatsTracker.DeactivatedField?.GetValue(gObject)) == true)
      return;

    StatsTracker.DayStats!.MissedItems.Add(new(
          gObject.gameObject.GetComponentInChildren<ScanNodeProperties>() == null ? 
            gObject.itemProperties.name : 
            gObject.gameObject.GetComponentInChildren<ScanNodeProperties>().headerText, 
          gObject.scrapValue,
          gObject.transform.position,
          !objectsExtraSpawnedThisDay.Contains(gObject.NetworkObject),
          StatsTracker.GiftBoxItemType?.IsInstanceOfType(gObject) == true ? 
            Traverse.Create(gObject).Field(nameof(GiftBoxItem.objectInPresentValue)).GetValue<int>() : 
            0));
  }

  public static void TrackCollectedItems()
  {
    GrabbableObject[] allObjs = Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
    foreach (GrabbableObject gObj in allObjs)
    {
      if (gObj.itemProperties.isScrap && !((bool?)(StatsTracker.DeactivatedField?.GetValue(gObj)) == true) && !gObj.itemUsedUp && gObj.isInShipRoom)
      {
        if (indexFromGiftBox.TryGetValue(gObj.NetworkObject, out int index))
          StatsTracker.DayStats!.GiftBoxesOpened[index].Collected = true;

        if (!objectsExtraSpawnedThisDay.Contains(gObj.NetworkObject))
          continue;

        StatsTracker.DayStats!.CollectedTotal += gObj.scrapValue;

        if (objectsNaturallySpawnedThisDay.Contains(gObj.NetworkObject))
          StatsTracker.DayStats!.CollectedNoExtra += gObj.scrapValue;
        else if (valueFromGiftSpawner.TryGetValue(gObj.NetworkObject, out int originalGitfValue))
          StatsTracker.DayStats!.CollectedNoExtra += originalGitfValue;
        else if (gObj.itemProperties.name == "RedLocustHive")
          StatsTracker.DayStats!.BeeInfo.AddToCollected(gObj.scrapValue);
        else if (StatsTracker.EggItemType?.IsInstanceOfType(gObj) == true)
          StatsTracker.DayStats!.EggInfo.AddToCollected(gObj.scrapValue);
        else if (StatsTracker.ShotgunItemType?.IsInstanceOfType(gObj) == true)
          StatsTracker.DayStats!.ShotgunInfo.AddToCollected(gObj.scrapValue);
        else if (StatsTracker.KnifeItemType?.IsInstanceOfType(gObj) == true)
          StatsTracker.DayStats!.KnifeInfo.AddToCollected(gObj.scrapValue);
      }
    }
  }

  public static void AddNewlySpawnedGiftItemToItemTracker(GiftBoxItem __instance, NetworkObjectReference netObjectRef)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    if (!StatsTracker.dayHasStarted)
      return;

    int indexForNewlyOpenedGift = StatsTracker.DayStats!.GiftBoxesOpened.Count;
    indexFromGiftBox[__instance.NetworkObject] = indexForNewlyOpenedGift;
    StatsTracker.DayStats!.GiftBoxesOpened.Add(new(__instance.objectInPresentValue, __instance.scrapValue));
    
    // Using StartOfRound to make sure the coroutine doesn't get interrupted early if the gift instance is destroyed somehow
    StartOfRound.Instance.StartCoroutine(WaitForGiftItemToFullySpawnBeforeTracking(netObjectRef, indexForNewlyOpenedGift, __instance.scrapValue, __instance.objectInPresentValue, objectsExtraSpawnedThisDay.Contains(__instance.NetworkObject)));
  }

  private static IEnumerator WaitForGiftItemToFullySpawnBeforeTracking(NetworkObjectReference netObjRef, int indexForCollectedGift, int originalGiftValue, int newScrapValue, bool giftSpawnedThisDay)
  {
    NetworkObject netObject = null!;
    float startTime = Time.realtimeSinceStartup;
    yield return new WaitUntil(() => Time.realtimeSinceStartup - startTime < 8f && !netObjRef.TryGet(out netObject));
    if (netObject == null)
    {
      StatsTracker.Logger.LogWarning("No network object found for giftbox");
      yield break;
    }

    indexFromGiftBox[netObjRef] = indexForCollectedGift;
    if (giftSpawnedThisDay)
    {
      valueFromGiftSpawner[netObjRef] = originalGiftValue;
      StatsTracker.DayStats!.BottomLineTrue += newScrapValue - originalGiftValue;
    }
    else
    {
      objectsExtraSpawnedThisDay.Remove(netObjRef);
      StatsTracker.DayStats!.ExtraFromOldGift += newScrapValue - originalGiftValue;
    }
  }

  public static void TrackHive(RedLocustBees __instance, int hiveScrapValue)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats!.BeeInfo.AddToAvailable(hiveScrapValue);
  }

  public static void TrackKnifeBeforePopping(ButlerEnemyAI __instance)
  {
    StatsTracker.DayStats!.BottomLineTrue += knifeValue;
  }

  public static void TrackButlerPopAndRemoveFakeValue(ButlerEnemyAI __instance)
  {
    StatsTracker.DayStats!.BottomLineTrue -= knifeValue;
  }

  public static void TrackEggs(GiantKiwiAI __instance, int[] eggScrapValues)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    foreach (int eggValue in eggScrapValues)
    {
      StatsTracker.DayStats!.EggInfo.AddToAvailable(eggValue);
    }
  }
}
