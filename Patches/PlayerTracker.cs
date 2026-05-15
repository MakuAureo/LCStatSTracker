using GameNetcodeStuff;
using Unity.Netcode;

namespace StatsTracker.Patches;

internal class PlayerTracker
{
  public static void TrackDeath(PlayerControllerB __instance, int causeOfDeath)
  {
    if ((GameNetworkManager.Instance.gameVersionNum > 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Execute) || (GameNetworkManager.Instance.gameVersionNum <= 72 && __instance.__rpc_exec_stage != NetworkBehaviour.__RpcExecStage.Client))
      return;

    StatsTracker.DayStats!.Players[__instance.playerSteamId]!
      .Kill(StatsTracker.GetCurrentTimeString(),
          ((CauseOfDeath)causeOfDeath).ToString());
  }

  public static void TrackDisconnect(StartOfRound __instance, int playerObjectNumber)
  {
    StatsTracker.DayStats!.Players[__instance.allPlayerScripts[playerObjectNumber].playerSteamId].Disconnect();
  }
}
