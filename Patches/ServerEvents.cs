using System;
using System.Collections;
using HarmonyLib;
using Newtonsoft.Json;
using Unity.Netcode;
using UnityEngine;

namespace StatsTracker.Patches;

internal class ServerEvents 
{
  public static void ApplyServerEventPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.ResetPlayersLoadedValueClientRpc)), prefix: new HarmonyMethod(typeof(ServerEvents), nameof(StartTrackingNewday)));
    Harmony.Patch(AccessTools.Method(typeof(RoundManager), nameof(RoundManager.FinishGeneratingNewLevelClientRpc)), prefix: new HarmonyMethod(typeof(ServerEvents), nameof(TrackNewSeed)));
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.PassTimeToNextDay)), postfix: new HarmonyMethod(typeof(ServerEvents), nameof(PublishDayStats)));
  }

  private static void StartTrackingNewday(StartOfRound __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats = new(GameNetworkManager.Instance.gameVersionNum, __instance.currentLevel.PlanetName,
        __instance.currentLevel.currentWeather == LevelWeatherType.None ? "Mild" : __instance.currentLevel.currentWeather.ToString(),
        new ArraySegment<GameNetcodeStuff.PlayerControllerB>(__instance.allPlayerScripts, 0, __instance.connectedPlayersAmount + 1).ToArray());

    StatsTracker.dayHasStarted = true;

    TimeOfDay.Instance.normalizedTimeOfDay = 0;
  }

  private static void TrackNewSeed(RoundManager __instance)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats!.Seed = StartOfRound.Instance.randomMapSeed;
  }

  private static void PublishDayStats(StartOfRound __instance)
  {
    if (TimeOfDay.Instance.profitQuota - TimeOfDay.Instance.quotaFulfilled <= 0)
      __instance.StartCoroutine(PublishDayStatsAfterQuotaRoll(TimeOfDay.Instance.profitQuota));
    else
      StatsTracker.LocalServer.PublishStats(JsonConvert.SerializeObject(StatsTracker.DayStats));

    StatsTracker.dayHasStarted = false;
  }

  private static IEnumerator PublishDayStatsAfterQuotaRoll(int prevQuota)
  {
    yield return new WaitUntil(() => TimeOfDay.Instance.profitQuota != prevQuota);
    StatsTracker.DayStats!.NewQuota = TimeOfDay.Instance.profitQuota;
    StatsTracker.LocalServer.PublishStats(JsonConvert.SerializeObject(StatsTracker.DayStats));
  }
}
