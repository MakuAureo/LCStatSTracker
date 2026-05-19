using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
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

  public static void ApplyItemAndEventTrackerPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(ResetItemAndEventTrackerWhenStartingNewDay)));

    MethodInfo RoundManagerSyncScrapValuesClientRpc = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.SyncScrapValuesClientRpc));
    Harmony.Patch(RoundManagerSyncScrapValuesClientRpc, prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackDungeonInfo)));
    Harmony.Patch(RoundManagerSyncScrapValuesClientRpc, prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackSID)));

    MethodInfo RoundManagerRefreshEnemiesList = AccessTools.Method(typeof(RoundManager), nameof(RoundManager.RefreshEnemiesList));
    FieldInfo? enemyRushIndexField = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.enemyRushIndex));
    if (enemyRushIndexField != null)
      enemyRushIndexPath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void enemyRushIndexPath() => Harmony.Patch(RoundManagerRefreshEnemiesList, postfix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackInfestation)));

    FieldInfo? indoorFogInfo = AccessTools.Field(typeof(RoundManager), nameof(RoundManager.indoorFog));
    if (indoorFogInfo != null)
      indoorFogIntoPath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void indoorFogIntoPath() => Harmony.Patch(RoundManagerRefreshEnemiesList, postfix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackIndoorFog)));

    MethodInfo? TimeOfDaySetBeginMeteorShowerClientRpcInfo = AccessTools.Method(typeof(TimeOfDay), nameof(TimeOfDay.SetBeginMeteorShowerClientRpc));
    if (TimeOfDaySetBeginMeteorShowerClientRpcInfo != null)
      TimeOfDaySetBeginMeteorShowerClientRpcInfoPath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void TimeOfDaySetBeginMeteorShowerClientRpcInfoPath() => Harmony.Patch(TimeOfDaySetBeginMeteorShowerClientRpcInfo, prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackMeteorShower)));

    Harmony.Patch(AccessTools.Method(typeof(LungProp), nameof(LungProp.Start)), postfix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(CountApp)));
    Harmony.Patch(AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkSpawn)), postfix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackSpawnedItems)));
    Harmony.Patch(AccessTools.Method(typeof(NetworkBehaviour), nameof(NetworkBehaviour.OnNetworkDespawn)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackMissedItems)));
    Harmony.Patch(AccessTools.Method(typeof(RoundManager), nameof(RoundManager.DespawnPropsAtEndOfRound)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackCollectedItems)));
  
    if (StatsTracker.GiftBoxItemType != null) 
      GiftBoxItemTypePath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void GiftBoxItemTypePath()
    {
      Harmony.Patch(AccessTools.Method(StatsTracker.GiftBoxItemType, nameof(GiftBoxItem.OpenGiftBoxClientRpc)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(AddNewlySpawnedGiftItemToItemTracker)));

      MethodInfo GiftBoxItemInternalValueSet = AccessTools.Method(StatsTracker.GiftBoxItemType, nameof(GiftBoxItem.InitializeAfterPositioning)) ?? AccessTools.Method(StatsTracker.GiftBoxItemType, nameof(GiftBoxItem.Start));
      Harmony.Patch(GiftBoxItemInternalValueSet, postfix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(PopulateObjectInGiftValueForAllClients)));
    }

    Harmony.Patch(AccessTools.Method(typeof(RedLocustBees), nameof(RedLocustBees.SpawnHiveClientRpc)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackHive)));

    Type? NutcrackerEnemyAIType = AccessTools.TypeByName(nameof(NutcrackerEnemyAI));
    if (NutcrackerEnemyAIType != null)
      NutcrackerEnemyAITypePath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void NutcrackerEnemyAITypePath() => Harmony.Patch(AccessTools.Method(NutcrackerEnemyAIType, nameof(NutcrackerEnemyAI.InitializeNutcrackerValuesClientRpc)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackShotgun)));

    Type? ButlerEnemyAIType = AccessTools.TypeByName(nameof(ButlerEnemyAI));
    if (ButlerEnemyAIType != null)
      ButlerEnemyAITypePath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void ButlerEnemyAITypePath()
    {
      Harmony.Patch(AccessTools.Method(ButlerEnemyAIType, nameof(ButlerEnemyAI.Start)), postfix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackKnifeBeforePopping)));
      Harmony.Patch(AccessTools.Method(ButlerEnemyAIType, nameof(ButlerEnemyAI.KillEnemy)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackButlerPopAndRemoveFakeValue)));
    }

    Type? GiantKiwiAIType = AccessTools.TypeByName(nameof(GiantKiwiAI));
    if (GiantKiwiAIType != null)
      GiantKiwiAITypePath();

    [MethodImpl(MethodImplOptions.NoInlining | MethodImplOptions.NoOptimization)]
    void GiantKiwiAITypePath() => Harmony.Patch(AccessTools.Method(GiantKiwiAIType, nameof(GiantKiwiAI.SpawnEggsClientRpc)), prefix: new HarmonyMethod(typeof(ItemAndEventTracker), nameof(TrackEggs)));
  }

  private static void ResetItemAndEventTrackerWhenStartingNewDay(RoundManager __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    appSpawnedThisDay.Clear();
    objectsNaturallySpawnedThisDay.Clear();
    objectsExtraSpawnedThisDay.Clear();
    valueFromGiftSpawner.Clear();
    indexFromGiftBox.Clear();
  }

  private static void TrackDungeonInfo(RoundManager __instance, NetworkObjectReference[] spawnedScrap, int[] allScrapValue)
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

  private static void TrackSID(RoundManager __instance, NetworkObjectReference[] spawnedScrap)
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

  private static void TrackInfestation(RoundManager __instance)
  {
    if (__instance.enemyRushIndex != -1)
      StatsTracker.DayStats!.InfestationType = __instance.currentLevel.Enemies[__instance.enemyRushIndex].enemyType.name;
  }

  private static void TrackIndoorFog(RoundManager __instance)
  {
    StatsTracker.DayStats!.IndoorFog = __instance.indoorFog.gameObject.activeSelf;
  }

  private static void TrackMeteorShower(TimeOfDay __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats!.MeteorShowerTime = StatsTracker.GetCurrentTimeString();
  }

  private static void CountApp(LungProp __instance)
  {
    StatsTracker.DayStats!.AppSpawned = true;
    appSpawnedThisDay.Add(__instance.NetworkObject);
  }

  private static void TrackSpawnedItems(NetworkBehaviour __instance)
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
  }

  private static void PopulateObjectInGiftValueForAllClients(object __instance)
  {
    GiftBoxItem instance = (GiftBoxItem)__instance;

    System.Random randomSeed = new((int)instance.targetFloorPosition.x + (int)instance.targetFloorPosition.y);
    System.Random random = new((int)instance.targetFloorPosition.x + (int)instance.targetFloorPosition.y);

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
    instance.objectInPresentValue = (int)((float)random.Next(itemInGift.minValue + 25, itemInGift.maxValue + 35) * RoundManager.Instance.scrapValueMultiplier);
  }

  private static void TrackMissedItems(NetworkBehaviour __instance)
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

  private static void TrackCollectedItems()
  {
    GrabbableObject[] allObjs = UnityEngine.Object.FindObjectsByType<GrabbableObject>(FindObjectsSortMode.None);
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

  private static void AddNewlySpawnedGiftItemToItemTracker(object __instance, NetworkObjectReference netObjectRef)
  {
    GiftBoxItem instance = (GiftBoxItem)__instance;

    if ((GameNetworkManager.Instance.gameVersionNum > 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    if (!StatsTracker.dayHasStarted)
      return;

    int indexForNewlyOpenedGift = StatsTracker.DayStats!.GiftBoxesOpened.Count;
    StatsTracker.DayStats!.GiftBoxesOpened.Add(new(instance.objectInPresentValue, instance.scrapValue));
    
    // Using StartOfRound to make sure the coroutine doesn't get interrupted early if the gift instance is destroyed somehow
    StartOfRound.Instance.StartCoroutine(WaitForGiftItemToFullySpawnBeforeTracking(netObjectRef, indexForNewlyOpenedGift, instance.scrapValue, instance.objectInPresentValue, objectsExtraSpawnedThisDay.Contains(instance.NetworkObject)));
  }

  private static IEnumerator WaitForGiftItemToFullySpawnBeforeTracking(NetworkObjectReference netObjRef, int indexForCollectedGift, int originalGiftValue, int newScrapValue, bool giftSpawnedThisDay)
  {
    NetworkObject netObject = null!;
    float startTime = Time.realtimeSinceStartup;

    yield return new WaitWhile(() => Time.realtimeSinceStartup - startTime < 8f && !netObjRef.TryGet(out netObject));
    if (netObject == null)
    {
      StatsTracker.Logger.LogWarning("No network object found for giftbox");
      yield break;
    }

    indexFromGiftBox[netObject] = indexForCollectedGift;
    if (giftSpawnedThisDay)
    {
      valueFromGiftSpawner[netObject] = originalGiftValue;
      StatsTracker.DayStats!.BottomLineTrue += newScrapValue - originalGiftValue;
    }
    else
    {
      objectsExtraSpawnedThisDay.Remove(netObject);
      StatsTracker.DayStats!.ExtraFromOldGift += newScrapValue - originalGiftValue;
    }
  }

  private static void TrackHive(RedLocustBees __instance, int hiveScrapValue)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats!.BeeInfo.AddToAvailable(hiveScrapValue);
  }

  private static void TrackShotgun(NutcrackerEnemyAI __instance, NetworkObjectReference gunObject)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    NetworkObject gunObj = null!;
    gunObject.TryGet(out gunObj);
    if (gunObj == null)
    {
      StatsTracker.Logger.LogWarning("Gun in GrabGun function did not contain NetworkObject.");
      return;
    }

		ShotgunItem gun = gunObj.GetComponent<ShotgunItem>();
		if (gun == null)
		{
			StatsTracker.Logger.LogWarning("Gun in GrabGun function did not contain ShotgunItem component.");
			return;
		}

    StartOfRound.Instance.StartCoroutine(WaitUntilShotgunValueHasBeenSetBeforeAddingToAvailable(gun));
  }

  private static IEnumerator WaitUntilShotgunValueHasBeenSetBeforeAddingToAvailable(ShotgunItem instance)
  {
    yield return new WaitUntil(() => instance.scrapValue != 0);

    StatsTracker.DayStats!.ShotgunInfo.AddToAvailable(instance.scrapValue);
  }

  private static void TrackKnifeBeforePopping(ButlerEnemyAI __instance)
  {
    StatsTracker.DayStats!.BottomLineTrue += knifeValue;
    StatsTracker.DayStats!.KnifeInfo.AddToAvailable(knifeValue);
  }

  private static void TrackButlerPopAndRemoveFakeValue(ButlerEnemyAI __instance)
  {
    StatsTracker.DayStats!.BottomLineTrue -= knifeValue;
  }

  private static void TrackEggs(GiantKiwiAI __instance, int[] eggScrapValues)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    foreach (int eggValue in eggScrapValues)
    {
      StatsTracker.DayStats!.EggInfo.AddToAvailable(eggValue);
    }
  }
}
