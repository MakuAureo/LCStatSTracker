using GameNetcodeStuff;
using HarmonyLib;
using Unity.Netcode;

namespace StatsTracker.Patches;

internal class PlayerTracker
{
  public static void ApplyPlayerTrackerPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(PlayerControllerB), nameof(PlayerControllerB.KillPlayerClientRpc)), prefix: new HarmonyMethod(typeof(PlayerTracker), nameof(TrackDeath)));
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.OnPlayerDC)), prefix: new HarmonyMethod(typeof(PlayerTracker), nameof(TrackDisconnect)));
  }

  private static void TrackDeath(PlayerControllerB __instance, int causeOfDeath)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats!.Players[__instance.playerSteamId]!
      .Kill(StatsTracker.GetCurrentTimeString(),
          ((CauseOfDeath)causeOfDeath).ToString());
  }

  private static void TrackDisconnect(StartOfRound __instance, int playerObjectNumber)
  {
    StatsTracker.DayStats!.Players[__instance.allPlayerScripts[playerObjectNumber].playerSteamId].Disconnect();
  }
}
