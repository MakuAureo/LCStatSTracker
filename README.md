# StatTracker

StatTracker is a mod that allows you to see (almost) everything that happened during your day, it tracks everything automatically as you play and pushes all the info as a JSON at the end of the day to a local server.
This is meant to allow people to treat this data however they want.

# Local server

The local server is hosted on port 2145, it uses SSE and can be queried at any time but will only release the data after the current day ends.
The data can only be queried once per day, once it is sent the server will wait for the next day to finish.

Just HTTP request it and wait until the day is over to get your stats.

# Currently Tracked Stats
```
{
  "MoonInfo": {
    "Name": "56 Vow",
    "Weather": "Mild"
  },
  "DungeonInfo": {
    "ItemCount": 19,
    "Interior": "Mineshaft"
  },
  "HazardInfo": {
    "TurretCount": 0,
    "LandmineCount": 0,
    "SpiketrapCount": 0
  },
  "BeeInfo": {
    "Total": [
      57
    ],
    "Collected": [
      57
    ]
  },
  "EggInfo": {
    "Total": [],
    "Collected": []
  },
  "KnifeInfo": {
    "Total": [],
    "Collected": []
  },
  "ShotgunInfo": {
    "Total": [],
    "Collected": []
  },
  "Seed": 60286767,
  "CollectedNoExtra": 0,
  "CollectedTotal": 57,
  "BottomLine": 863,
  "BottomLineTrue": 917,
  "ValueSold": 0,
  "NewQuota": 0,
  "AppSpawned": false,
  "IndoorFog": false,
  "TakeOffTime": "9:00 AM",
  "SIDType": "",
  "InfestationType": "",
  "MeteorShowerTime": "",
  "Players": {
    "76561198980273231": {
      "Name": "AureoHatsune",
      "Alive": true,
      "Disconnected": false,
      "TimeOfDeath": "",
      "CauseOfDeath": ""
    }
  },
  "IndoorSpawns": [],
  "DayTimeSpawns": [
    {
      "Enemy": "Red Locust Bees",
      "SpawnTime": "7:40 AM",
      "TimeOfDeath": ""
    },
    {
      "Enemy": "Manticoil",
      "SpawnTime": "9:00 AM",
      "TimeOfDeath": ""
    },
    ...
  ],
  "NightTimeSpawns": [],
  "GiftBoxes": [
    {
      "GiftValue":21,
      "ScrapValue":18,
      "Collected":false
    }
  ],
  "MissedItems": [
    {
      "Value": 60,
      "ItemType": "Control pad",
      "DespawnPosition": [
        -18.2,
        -212.6,
        102.1
      ],
      "CollectedOnPreviousDay": false
    },
    {
      "Value": 59,
      "ItemType": "Brass bell",
      "DespawnPosition": [
        -57.5,
        -219,
        105.3
      ],
      "CollectedOnPreviousDay": false
    },
    ...
  ]
}
```
