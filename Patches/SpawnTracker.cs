using System;
using System.Reflection;
using HarmonyLib;
using Unity.Netcode;

namespace StatsTracker.Patches;

[HarmonyPatch]
internal class SpawnTracker
{
  private const int knifeValue = 35;
  
  [HarmonyPatch(typeof(EnemyAI), nameof(EnemyAI.Start))]
  [HarmonyPostfix]
  private static void TrackSpawn(EnemyAI __instance)
  {
    if (__instance.enemyType.isDaytimeEnemy)
      StatsTracker.DayStats?.DayTimeSpawns.Add(new(__instance.enemyType, StatsTracker.GetCurrentTimeString()));
    else if (__instance.enemyType.isOutsideEnemy)
      StatsTracker.DayStats?.NightTimeSpawns.Add(new(__instance.enemyType, StatsTracker.GetCurrentTimeString()));
    else
      StatsTracker.DayStats?.IndoorSpawns.Add(new(__instance.enemyType, StatsTracker.GetCurrentTimeString()));
  }

  [HarmonyPatch(typeof(RedLocustBees), nameof(RedLocustBees.SpawnHiveClientRpc))]
  [HarmonyPrefix]
  private static void TrackHive(RedLocustBees __instance, int hiveScrapValue)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats?.BeeInfo.AddBeeValue(hiveScrapValue);
    StatsTracker.DayStats?.BottomLineTrue += hiveScrapValue;
  }

  [HarmonyPatch]
  private static class NutcrackerWrapper
  {
    private static Type? NutcrackerEnemyAIType = null;
    private static bool Prepare()
    {
      NutcrackerEnemyAIType = AccessTools.TypeByName(nameof(NutcrackerEnemyAI));
      return (NutcrackerEnemyAIType != null);
    }
    private static MethodBase TargetMethod() => AccessTools.Method(NutcrackerEnemyAIType, nameof(NutcrackerEnemyAI.InitializeNutcrackerValuesClientRpc));
    private static void Prefix(object __instance, NetworkObjectReference gunObject) 
    {
      NutcrackerEnemyAI instance = (NutcrackerEnemyAI)(__instance);

      if ((GameNetworkManager.Instance.gameVersionNum > 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
        return;

      gunObject.TryGet(out NetworkObject gunNetObj);
      ShotgunItem gun = gunNetObj.GetComponent<ShotgunItem>();
      if (gun == null)
      {
        StatsTracker.Logger.LogWarning("Unable to retrieve ShotgunItem from the spawned Nutcracker");
        return;
      }

      StatsTracker.DayStats?.BottomLineTrue += gun.scrapValue;
    }
  }

  [HarmonyPatch]
  private static class ButlerWrapper
  {
    private static Type? ButlerEnemyAIType = null;
    private static bool Prepare()
    {
      ButlerEnemyAIType = AccessTools.TypeByName(nameof(ButlerEnemyAI));
      return (ButlerEnemyAIType != null);
    }
    private static MethodBase TargetMethod() => AccessTools.Method(ButlerEnemyAIType, nameof(ButlerEnemyAI.Start));
    private static void Prefix(object __instance) 
    {
      StatsTracker.DayStats?.BottomLineTrue += knifeValue;
    }
  }

  [HarmonyPatch]
  private static class GiantKiwiAIWrapper
  {
    private static Type? GiantKiwiAIType = null;
    private static bool Prepare()
    {
      GiantKiwiAIType = AccessTools.TypeByName(nameof(GiantKiwiAI));
      return (GiantKiwiAIType != null);
    }
    private static MethodBase TargetMethod() => AccessTools.Method(GiantKiwiAIType, nameof(GiantKiwiAI.SpawnEggsClientRpc));
    private static void Prefix(object __instance,  int[] eggScrapValues) 
    {
      GiantKiwiAI instance = (GiantKiwiAI)__instance;

      if ((GameNetworkManager.Instance.gameVersionNum > 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
        return;

      StatsTracker.DayStats?.BirdInfo.AddEggValue(eggScrapValues);
      foreach (int eggValue in eggScrapValues)
        StatsTracker.DayStats?.BottomLineTrue += eggValue;
    }
  }
}
