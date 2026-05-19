using System.Collections.Generic;
using HarmonyLib;
using Unity.Netcode;

namespace StatsTracker.Patches;

internal class SpawnTracker
{
  public static void ApplySpawnTrackerPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(RoundManager), nameof(RoundManager.GenerateNewLevelClientRpc)), prefix: new HarmonyMethod(typeof(SpawnTracker), nameof(ResetSpawnTrackerWhenStartingNewDay)));
    Harmony.Patch(AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.Start)), postfix: new HarmonyMethod(typeof(SpawnTracker), nameof(TrackSpawn)));
    Harmony.Patch(AccessTools.Method(typeof(EnemyAI), nameof(EnemyAI.KillEnemy)), prefix: new HarmonyMethod(typeof(SpawnTracker), nameof(TrackDeath)));
  }

  private static readonly Dictionary<NetworkObjectReference, int> EnemyToSpawnInfoIndex = [];

  private static void ResetSpawnTrackerWhenStartingNewDay(RoundManager __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    EnemyToSpawnInfoIndex.Clear();
  }
   
  // CURRENT ISSUE: WILL NOT TRACK MODDED SPAWNS CORRECTLY (PROB NEED STARLANCER_AI_FIX DEPEDENCY)
  private static void TrackSpawn(EnemyAI __instance)
  {
    if (__instance.enemyType.isDaytimeEnemy)
    {
      EnemyToSpawnInfoIndex[__instance.NetworkObject] = StatsTracker.DayStats!.DayTimeSpawns.Count;
      StatsTracker.DayStats!.DayTimeSpawns.Add(new(__instance.enemyType, StatsTracker.GetCurrentTimeString()));
    }
    else if (__instance.enemyType.isOutsideEnemy)
    {
      EnemyToSpawnInfoIndex[__instance.NetworkObject] = StatsTracker.DayStats!.NightTimeSpawns.Count;
      StatsTracker.DayStats!.NightTimeSpawns.Add(new(__instance.enemyType, StatsTracker.GetCurrentTimeString()));
    }
    else
    {
      EnemyToSpawnInfoIndex[__instance.NetworkObject] = StatsTracker.DayStats!.IndoorSpawns.Count;
      StatsTracker.DayStats!.IndoorSpawns.Add(new(__instance.enemyType, StatsTracker.GetCurrentTimeString()));
    }
  }

  private static void TrackDeath(EnemyAI __instance)
  {
    EnemyToSpawnInfoIndex.TryGetValue(__instance.NetworkObject, out int index);
    try {
      if (__instance.enemyType.isDaytimeEnemy)
      {
        StatsTracker.DayStats!.DayTimeSpawns[index].TimeOfDeath = StatsTracker.GetCurrentTimeString();
      }
      else if (__instance.enemyType.isOutsideEnemy)
      {
        StatsTracker.DayStats!.NightTimeSpawns[index].TimeOfDeath = StatsTracker.GetCurrentTimeString();
      }
      else
      {
        StatsTracker.DayStats!.IndoorSpawns[index].TimeOfDeath = StatsTracker.GetCurrentTimeString();
      }
    } catch (System.Exception e) {
      StatsTracker.Logger.LogError($"Error when registering enemy death: {e.Message}\n${e.StackTrace}");
    }
  }
}
