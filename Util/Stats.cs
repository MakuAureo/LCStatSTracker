using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatsTracker.Util;

internal class PlayerStats(string name)
{
  public string Name = name;
  public bool Alive = true;
  public bool Disconnected = false;
  public string TimeOfDeath = "";
  public string CauseOfDeath = "";

  public void Kill(string TimeOfDeath, string CauseOfDeath)
  {
    Alive = false;
    this.TimeOfDeath = TimeOfDeath;
    this.CauseOfDeath = CauseOfDeath;
  }

  public void Disconnect()
  {
    Disconnected = true;
  }
}

internal class SpecialItemInfo()
{
  public List<int> Available = [];
  public List<int> Collected = [];

  public void AddToAvailable(int value)
  {
    Available.Add(value);
  }

  public void AddToCollected(int value)
  {
    Collected.Add(value);
  }
}

internal class GiftBoxInfo(int GiftValue, int ScrapValue)
{
  public int GiftValue = GiftValue;
  public int ScrapValue = ScrapValue;
  public bool Collected = false;
}

internal class MissingItemInfo(string Name, int Value, Vector3 DespawnPosition, bool CollectedOnPreviousDay, int? ScrapInsideGiftValue = null)
{
  public int Value = Value;
  public string ItemType = Name;
  public double[] DespawnPosition = [Math.Round(DespawnPosition.x, 1), Math.Round(DespawnPosition.y, 1), Math.Round(DespawnPosition.z, 1)];
  public bool CollectedOnPreviousDay = CollectedOnPreviousDay;
  public int? ScrapInsideGiftValue = ScrapInsideGiftValue;
}

internal class HazardInfo(int TurretCount, int LandmineCount, int SpiketrapCount)
{
  public int TurretCount = TurretCount;
  public int LandmineCount = LandmineCount;
  public int SpiketrapCount = SpiketrapCount;
}

internal class MoonInfo(string Name, string Weather)
{
  public string Name = Name;
  public string Weather = Weather;
}

internal class DungeonInfo(int ItemCount, string Interior)
{
  public int ItemCount = ItemCount;
  public string Interior = Interior;
}

internal class SpawnInfo(EnemyType EnemyType, string Time)
{
  public string Enemy = EnemyType.enemyName;
  public string SpawnTime = Time;
  public string TimeOfDeath = "";
}

internal class Stats
{
  public MoonInfo MoonInfo;
  public DungeonInfo? DungeonInfo;
  public HazardInfo? HazardInfo;

  public SpecialItemInfo BeeInfo;
  public SpecialItemInfo EggInfo;
  public SpecialItemInfo KnifeInfo;
  public SpecialItemInfo ShotgunInfo;

  public int Seed;
  public int Version;

  public int CollectedNoExtra;
  public int CollectedTotal;
  public int BottomLine;
  public int BottomLineTrue;
  public int ExtraFromOldGift;

  public int ValueSold;
  public int NewQuota;

  public bool AppSpawned;
  public bool IndoorFog;
  public string TakeOffTime;
  public string SIDType;
  public string InfestationType;
  public string MeteorShowerTime;

  public Dictionary<ulong, PlayerStats> Players;

  public List<SpawnInfo> IndoorSpawns;
  public List<SpawnInfo> DayTimeSpawns;
  public List<SpawnInfo> NightTimeSpawns;

  public List<GiftBoxInfo> GiftBoxesOpened;
  public List<MissingItemInfo> MissedItems;

  public Stats(int seed, int version, string moonName, string weather, GameNetcodeStuff.PlayerControllerB[] allPlayers)
  {
    MoonInfo = new(moonName, weather);
    BeeInfo = new();
    EggInfo = new();
    KnifeInfo = new();
    ShotgunInfo = new();
    Seed = seed;
    Version = version;
    CollectedNoExtra = 0;
    CollectedTotal = 0;
    BottomLine = 0;
    BottomLineTrue = 0;
    ExtraFromOldGift = 0;
    ValueSold = 0;
    NewQuota = 0;
    AppSpawned = false;
    IndoorFog = false;
    TakeOffTime = "";
    SIDType = "";
    InfestationType = "";
    MeteorShowerTime = "";
    Players = [];
    IndoorSpawns = [];
    DayTimeSpawns = [];
    NightTimeSpawns = [];
    GiftBoxesOpened = [];
    MissedItems = [];

    foreach (GameNetcodeStuff.PlayerControllerB player in allPlayers)
      Players[player.playerSteamId] = new(player.playerUsername);
  }
}
