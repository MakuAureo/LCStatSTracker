extern alias HQoL72;
extern alias HQoL73;

using HarmonyLib;
using UnityEngine;

namespace StatsTracker.Patches;

internal class CompanyTracker
{
  public static void ApplyCompanyTrackerPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(DepositItemsDesk), nameof(DepositItemsDesk.Update)), postfix: new HarmonyMethod(typeof(CompanyTracker), nameof(CheckForNewItemsOnCounter)));
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft)), prefix: new HarmonyMethod(typeof(CompanyTracker), nameof(ResetItemCountAfterTakingOffCompanyTypeMoon)));
  }

  private static int itemsOnCounterCountLastFrame = 0;

  private static void CheckForNewItemsOnCounter(DepositItemsDesk __instance)
  {
    if (__instance.itemsOnCounter.Count <= itemsOnCounterCountLastFrame)
      goto UpdateItemsOnCounterCount;
    
    for (int i = itemsOnCounterCountLastFrame; i < __instance.itemsOnCounter.Count; i++)
    {
      if (!__instance.itemsOnCounter[i].itemProperties.isScrap)
        continue;
      else
        StatsTracker.DayStats!.ValueSold += __instance.itemsOnCounter[i].scrapValue;
    }

    UpdateItemsOnCounterCount:
    itemsOnCounterCountLastFrame = __instance.itemsOnCounter.Count;
  }

  private static void ResetItemCountAfterTakingOffCompanyTypeMoon(RoundManager __instance)
  {
    if (Object.FindAnyObjectByType<DepositItemsDesk>() == null)
      return;

    itemsOnCounterCountLastFrame = 0;
  }
}

internal class HQoLTracker
{
  public static void ApplyHQoLTrackerPatches(Harmony Harmony)
  {
    Harmony.Patch(AccessTools.Method(typeof(DepositItemsDesk), nameof(DepositItemsDesk.Start)), postfix: new HarmonyMethod(typeof(HQoLTracker), nameof(RegisterOnChangeWhenLandingOnCompanyTypeMoon)));
    Harmony.Patch(AccessTools.Method(typeof(StartOfRound), nameof(StartOfRound.ShipHasLeft)), prefix: new HarmonyMethod(typeof(HQoLTracker), nameof(DeregisterOnChangeAfterTakingOffCompanyTypeMoon)));
  }

  private static int totalSold = 0;

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

    StatsTracker.DayStats!.ValueSold = totalSold;
    totalSold = 0;
  }

  private static void OnChangeFindSoldValue(int prevValue, int currValue)
  {
    if (currValue < prevValue)
      totalSold += prevValue - currValue;
  }
}
