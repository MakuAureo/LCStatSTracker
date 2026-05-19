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
   "MoonInfo":{
      "Name":"7 Dine",
      "Weather":"Flooded"
   },
   "DungeonInfo":{
      "ItemCount":24,
      "Interior":"Mansion"
   },
   "HazardInfo":{
      "TurretCount":0,
      "LandmineCount":2,
      "SpiketrapCount":0
   },
   "BeeInfo":{
      "Available":[
         
      ],
      "Collected":[
         
      ]
   },
   "EggInfo":{
      "Available":[
         
      ],
      "Collected":[
         
      ]
   },
   "KnifeInfo":{
      "Available":[
         35,
         35,
         35,
         35,
         35,
         35,
         35
      ],
      "Collected":[
         35,
         35,
         35
      ]
   },
   "ShotgunInfo":{
      "Available":[
         60,
         60
      ],
      "Collected":[
         60,
         60
      ]
   },
   "Seed":30494987,
   "Version":50,
   "CollectedNoExtra":0,
   "CollectedTotal":225,
   "BottomLine":1367,
   "BottomLineTrue":1732,
   "ExtraFromOldGift":0,
   "ValueSold":0,
   "NewQuota":0,
   "AppSpawned":false,
   "IndoorFog":false,
   "TakeOffTime":"5:25 PM",
   "SIDType":"",
   "InfestationType":"",
   "MeteorShowerTime":"",
   "Players":{
      "76561198850929220":{
         "Name":"wafrody",
         "Alive":true,
         "Disconnected":false,
         "TimeOfDeath":"",
         "CauseOfDeath":""
      },
      "76561198980273231":{
         "Name":"AureoHatsune",
         "Alive":true,
         "Disconnected":false,
         "TimeOfDeath":"",
         "CauseOfDeath":""
      }
   },
   "IndoorSpawns":[
      {
         "Enemy":"Puffer",
         "SpawnTime":"8:30 AM",
         "TimeOfDeath":""
      },
      {
         "Enemy":"Butler",
         "SpawnTime":"9:44 AM",
         "TimeOfDeath":"9:55 AM"
      },
      {
         "Enemy":"Butler Bees",
         "SpawnTime":"9:56 AM",
         "TimeOfDeath":""
      },
      {
         "Enemy":"Puffer",
         "SpawnTime":"10:25 AM",
         "TimeOfDeath":""
      },
      ...
   ],
   "DayTimeSpawns":[
      
   ],
   "NightTimeSpawns":[
      {
         "Enemy":"ForestGiant",
         "SpawnTime":"9:44 AM",
         "TimeOfDeath":""
      },
      {
         "Enemy":"MouthDog",
         "SpawnTime":"9:44 AM",
         "TimeOfDeath":""
      }
   ],
   "GiftBoxesOpened":[
      {
         "NewScrapValue":39,
         "GiftScrapValue":12,
         "Collected":false
      },
      {
         "NewScrapValue":162,
         "GiftScrapValue":26,
         "Collected":true
      }
   ],
   "MissedItems":[
      {
         "Value":35,
         "ItemType":"Kitchen knife",
         "DespawnPosition":[
            3.5,
            -209.5,
            54.5
         ],
         "CollectedOnPreviousDay":false,
         "ScrapInsideGiftValue":0
      },
      {
         "Value":35,
         "ItemType":"Kitchen knife",
         "DespawnPosition":[
            -11.8,
            -0.8,
            -13.2
         ],
         "CollectedOnPreviousDay":false,
         "ScrapInsideGiftValue":0
      },
      {
         "Value":35,
         "ItemType":"Kitchen knife",
         "DespawnPosition":[
            -77.9,
            -209.5,
            -40.6
         ],
         "CollectedOnPreviousDay":false,
         "ScrapInsideGiftValue":0
      },
      {
         "Value":35,
         "ItemType":"Kitchen knife",
         "DespawnPosition":[
            -83,
            -209.6,
            48.9
         ],
         "CollectedOnPreviousDay":false,
         "ScrapInsideGiftValue":0
      },
      ...
   ]
}
```
