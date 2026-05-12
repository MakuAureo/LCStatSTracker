using System;
using System.Collections;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

namespace StatsTracker.Patches;

internal class ServerEvents 
{
  public static void StartTrackingNewday(StartOfRound __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats = new(__instance.randomMapSeed, __instance.currentLevel.PlanetName,
        __instance.currentLevel.currentWeather == LevelWeatherType.None ? "Mild" : __instance.currentLevel.currentWeather.ToString(),
        new ArraySegment<GameNetcodeStuff.PlayerControllerB>(__instance.allPlayerScripts, 0, __instance.connectedPlayersAmount + 1).ToArray());
  }

  public static void PublishDayStats(StartOfRound __instance)
  {
    if (TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled <= 0)
      __instance.StartCoroutine(PublishDayStatsAfterQuotaRoll(TimeOfDay.Instance.profitQuota));
    else
      StatsTracker.LocalServer.PublishStats(JsonConvert.SerializeObject(StatsTracker.DayStats));
  }

  private static IEnumerator PublishDayStatsAfterQuotaRoll(int prevQuota)
  {
    yield return new WaitUntil(() => TimeOfDay.Instance.profitQuota != prevQuota);
    StatsTracker.DayStats?.NewQuota = TimeOfDay.Instance.profitQuota;
    StatsTracker.LocalServer.PublishStats(JsonConvert.SerializeObject(StatsTracker.DayStats));
  }
}
