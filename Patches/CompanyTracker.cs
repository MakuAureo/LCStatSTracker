extern alias HQoL72;
extern alias HQoL73;

using HarmonyLib;
using UnityEngine;

namespace StatsTracker.Patches;

[HarmonyPatch]
internal class CompanyTracker
{
  // Despite the name this run on every client, just early returns on client
  [HarmonyPatch(typeof(DepositItemsDesk), nameof(DepositItemsDesk.SellItemsOnServer))]
  [HarmonyPrefix]
  private static void CalculateAmountSold(DepositItemsDesk __instance)
  {
    for (int i = 0; i < __instance.itemsOnCounter.Count; i++)
    {
      if (!__instance.itemsOnCounter[i].itemProperties.isScrap)
        continue;
      else
        StatsTracker.DayStats?.ValueSold += __instance.itemsOnCounter[i].scrapValue;
    }
  }
}

internal class HQoLTracker
{
  private static int totalSold = 0;

  [HarmonyPatch(typeof(DepositItemsDesk), nameof(DepositItemsDesk.Start))]
  [HarmonyPostfix]
  private static void RegisterOnChangeWhenLandingOnCompanyTypeMoon(DepositItemsDesk __instance)
  {
    if (HQoL72.HQoL.Network.HQoLNetwork.Instance != null)
    {
      HQoL72.HQoL.Network.HQoLNetwork.Instance.totalStorageValue.OnValueChanged += OnChangeFindSoldValue;
    }
    else if (HQoL73.HQoL.Network.HQoLNetwork.Instance != null)
    {
      HQoL73.HQoL.Network.HQoLNetwork.Instance.totalStorageValue.OnValueChanged += OnChangeFindSoldValue;
    }
    else
    {
      StatsTracker.Logger.LogWarning("Failed to find HQoL instance, sold value will not be sync'd");
    }
  }

  [HarmonyPatch(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft))]
  [HarmonyPrefix]
  private static void DeregisterOnChangeAfterTakingOffCompanyTypeMoon(RoundManager __instance)
  {
    if (Object.FindAnyObjectByType<DepositItemsDesk>() == null)
      return;

    if (HQoL72.HQoL.Network.HQoLNetwork.Instance != null)
    {
      HQoL72.HQoL.Network.HQoLNetwork.Instance.totalStorageValue.OnValueChanged -= OnChangeFindSoldValue;
    }
    else if (HQoL73.HQoL.Network.HQoLNetwork.Instance != null)
    {
      HQoL73.HQoL.Network.HQoLNetwork.Instance.totalStorageValue.OnValueChanged -= OnChangeFindSoldValue;
    }
    else
    {
      StatsTracker.Logger.LogWarning("Failed to find HQoL instance, sold value will not be sync'd");
    }

    StatsTracker.DayStats?.ValueSold = totalSold;
    totalSold = 0;
  }

  private static void OnChangeFindSoldValue(int prevValue, int currValue)
  {
    if (currValue < prevValue)
      totalSold += prevValue - currValue;
  }
}
