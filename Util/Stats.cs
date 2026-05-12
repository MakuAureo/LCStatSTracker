using System;
using System.Collections.Generic;
using UnityEngine;

namespace StatsTracker.Util;

internal class PlayerStats
{
  public string Name;
  public bool Alive;
  public bool Disconnected;
  public string TimeOfDeath;
  public string CauseOfDeath;

  public PlayerStats(string name)
  {
    Name = name;
    Alive = true;
    Disconnected = false;
    TimeOfDeath = "";
    CauseOfDeath = "";
  }

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

internal class SpecialItemInfo
{
  public List<int> Available;
  public List<int> Collected;

  public SpecialItemInfo()
  {
    this.Available = new();
    this.Collected = new();
  }

  public void AddToAvailable(int value)
  {
    Available.Add(value);
  }

  public void AddToCollected(int value)
  {
    Collected.Add(value);
  }
}

internal class GiftBoxInfo
{
  public int GiftValue;
  public int ScrapValue;
  public bool Collected;

  public GiftBoxInfo(int GiftValue, int ScrapValue)
  {
    this.GiftValue = GiftValue;
    this.ScrapValue = ScrapValue;
    this.Collected = false;
  }
}

internal class MissingItemInfo
{
  public int Value;
  public string ItemType;
  public double[] DespawnPosition;
  public bool CollectedOnPreviousDay;

  public MissingItemInfo(string Name, int Value, Vector3 DespawnPosition, bool CollectedOnPreviousDay)
  {
    this.ItemType = Name;
    this.Value = Value;
    this.DespawnPosition = [ Math.Round(DespawnPosition.x, 1), Math.Round(DespawnPosition.y, 1), Math.Round(DespawnPosition.z, 1) ];
    this.CollectedOnPreviousDay = CollectedOnPreviousDay;
  }
}

internal class HazardInfo
{
  public int TurretCount;
  public int LandmineCount;
  public int SpiketrapCount;

  public HazardInfo(int TurretCount, int LandmineCount, int SpiketrapCount)
  {
    this.TurretCount = TurretCount;
    this.LandmineCount = LandmineCount;
    this.SpiketrapCount = SpiketrapCount;
  }
}

internal class MoonInfo
{
  public string Name;
  public string Weather;

  public MoonInfo(string Name, string Weather) 
  {
    this.Name = Name;
    this.Weather = Weather;
  }
}

internal class DungeonInfo
{
  public int ItemCount;
  public string Interior;

  public DungeonInfo(int ItemCount, string Interior)
  {
    this.ItemCount = ItemCount;
    this.Interior = Interior;
  }
}

internal class SpawnInfo
{
  public string Enemy;
  public string SpawnTime;
  public string TimeOfDeath;

  public SpawnInfo(EnemyType EnemyType, string Time)
  {
    this.Enemy = EnemyType.enemyName;
    this.SpawnTime = Time;
    this.TimeOfDeath = "";
  }
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

  public int CollectedNoExtra;
  public int CollectedTotal;
  public int BottomLine;
  public int BottomLineTrue;

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

  public List<GiftBoxInfo> GiftBoxes;
  public List<MissingItemInfo> MissedItems;

  public Stats(int seed, string moonName, string weather, GameNetcodeStuff.PlayerControllerB[] allPlayers)
  {
    MoonInfo = new(moonName, weather);
    BeeInfo = new();
    EggInfo = new();
    KnifeInfo = new();
    ShotgunInfo = new();
    Seed = seed;
    CollectedNoExtra = 0;
    CollectedTotal = 0;
    BottomLine = 0;
    BottomLineTrue = 0;
    ValueSold = 0;
    NewQuota = 0;
    AppSpawned = false;
    IndoorFog = false;
    TakeOffTime = "";
    SIDType = "";
    InfestationType = "";
    MeteorShowerTime = "";
    Players = new();
    IndoorSpawns = new();
    DayTimeSpawns = new();
    NightTimeSpawns = new();
    GiftBoxes = new();
    MissedItems = new();

    foreach (GameNetcodeStuff.PlayerControllerB player in allPlayers)
      Players[player.playerSteamId] = new(player.playerUsername);
  }
}
