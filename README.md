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
    "Name": "68 Artifice",
    "Weather": "Eclipsed"
  },
  "DungeonInfo": {
    "ItemCount": 34,
    "Interior": "Mineshaft"
  },
  "HazardInfo": {
    "TurretCount": 1,
    "LandmineCount": 2,
    "SpiketrapCount": 0
  },
  "BeeInfo": {
    "Available": [],
    "Collected": []
  },
  "EggInfo": {
    "Available": [],
    "Collected": []
  },
  "KnifeInfo": {
    "Available": [
      35
    ],
    "Collected": [
      35
    ]
  },
  "ShotgunInfo": {
    "Available": [],
    "Collected": []
  },
  "Seed": 10183014,
  "CollectedNoExtra": 926,
  "CollectedTotal": 1016,
  "BottomLine": 2043,
  "BottomLineTrue": 2133,
  "ValueSold": 0,
  "NewQuota": 0,
  "AppSpawned": false,
  "IndoorFog": false,
  "TakeOffTime": "11:57 PM",
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
  "IndoorSpawns": [
    {
      "Enemy": "Butler",
      "SpawnTime": "9:16 AM",
      "TimeOfDeath": "10:12 PM"
    },
    {
      "Enemy": "Crawler",
      "SpawnTime": "9:50 AM",
      "TimeOfDeath": ""
    },
    {
      "Enemy": "Blob",
      "SpawnTime": "9:56 AM",
      "TimeOfDeath": ""
    },
    {
      "Enemy": "Jester",
      "SpawnTime": "10:41 AM",
      "TimeOfDeath": ""
    },
    {
      "Enemy": "Blob",
      "SpawnTime": "11:12 AM",
      "TimeOfDeath": ""
    },
    {
      "Enemy": "Spring",
      "SpawnTime": "1:28 PM",
      "TimeOfDeath": ""
    }
  ],
  "DayTimeSpawns": [
    {
      "Enemy": "Docile Locust Bees",
      "SpawnTime": "7:40 AM",
      "TimeOfDeath": "7:50 PM"
    }
  ],
  "NightTimeSpawns": [
    {
      "Enemy": "ForestGiant",
      "SpawnTime": "7:40 AM",
      "TimeOfDeath": "10:21 AM"
    },
    {
      "Enemy": "Earth Leviathan",
      "SpawnTime": "7:40 AM",
      "TimeOfDeath": ""
    },
    {
      "Enemy": "RadMech",
      "SpawnTime": "7:40 AM",
      "TimeOfDeath": ""
    },
    {
      "Enemy": "MouthDog",
      "SpawnTime": "9:56 AM",
      "TimeOfDeath": "4:47 PM"
    }
    ...
  ],
  "GiftBoxes": [
    {
      "GiftValue": 69,
      "ScrapValue": 14,
      "Collected": true
    }
  ],
  "MissedItems": [
    {
      "Value": 96,
      "ItemType": "Rubber ducky",
      "DespawnPosition": [
        -25.6,
        -219.6,
        8.1
      ],
      "CollectedOnPreviousDay": false
    },
    {
      "Value": 74,
      "ItemType": "Wedding ring",
      "DespawnPosition": [
        -86.6,
        -219.4,
        88.9
      ],
      "CollectedOnPreviousDay": false
    },
    ...
  ]
}
```
