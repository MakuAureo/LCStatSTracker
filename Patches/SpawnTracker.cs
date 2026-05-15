using System.Collections.Generic;
using Unity.Netcode;

namespace StatsTracker.Patches;

internal class SpawnTracker
{
  private static Dictionary<NetworkObjectReference, int> EnemyToSpawnInfoIndex = new();

  public static void ResetTrackerWhenStartingNewDay(RoundManager __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    EnemyToSpawnInfoIndex.Clear();
  }
   
  // CURRENT ISSUE: WILL NOT TRACK MODDED SPAWNS CORRECTLY (PROB NEED STARLANCER_AI_FIX DEPEDENCY)
  public static void TrackSpawn(EnemyAI __instance)
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

  public static void TrackDeath(EnemyAI __instance)
  {
    if (__instance.enemyType.isDaytimeEnemy)
    {
      EnemyToSpawnInfoIndex.TryGetValue(__instance.NetworkObject, out int index);
      StatsTracker.DayStats!.DayTimeSpawns[index].TimeOfDeath = StatsTracker.GetCurrentTimeString();
    }
    else if (__instance.enemyType.isOutsideEnemy)
    {
      EnemyToSpawnInfoIndex.TryGetValue(__instance.NetworkObject, out int index);
      StatsTracker.DayStats!.NightTimeSpawns[index].TimeOfDeath = StatsTracker.GetCurrentTimeString();
    }
    else
    {
      EnemyToSpawnInfoIndex.TryGetValue(__instance.NetworkObject, out int index);
      StatsTracker.DayStats!.IndoorSpawns[index].TimeOfDeath = StatsTracker.GetCurrentTimeString();
    }
  }
}
