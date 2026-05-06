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
    "MoonInfo":
    {   
        "Name":"220 Assurance",
        "Weather":"Mild"
    },
    "DungeonInfo":
    {
        "ItemCount":20,
        "Interior":"Mineshaft"
    },
    "HazardInfo":
    {
        "TurretCount":2,
        "LandmineCount":1,
        "SpiketrapCount":0
    },
    "BeeInfo":
    {
        "Values":
        [
            70,
            101,
            101
        ]
    },
    "BirdInfo":
    {
        "EggValues":
        [
            71,
            119,
            94
        ]
    },
    "Seed":29003587,
    "ShotgunsCollected":1,
    "KnivesCollected":0,
    "CollectedNoExtra":291,
    "CollectedTotal":806,
    "BottomLine":760,
    "BottomLineTrue":1428,
    "ValueSold":0,
    "NewQuota":0,
    "AppSpawned":false,
    "IndoorFog":false,
    "TakeOffTime":"8:30 PM"
    "SIDType":"",
    "InfestationType":"",
    "MeteorShowerTime":"",
    "Players":
    {
        "76561198980273231":
        {
            "Name":"AureoHatsune",
            "Alive":true,
            "Disconnected":false,
            "TimeOfDeath":"",
            "CauseOfDeath":""
        }
    },
    "IndoorSpawns":
    [
        {
            "Enemy":"Bunker Spider",
            "SpawnTime":"7:40 AM"
        },
        {
            "Enemy":"Maneater",
            "SpawnTime":"7:40 AM"
        },
        {
            "Enemy":"Centipede",
            "SpawnTime":"11:02 AM"
        },
        ...
    ],
    "DayTimeSpawns":
    [
        {
            "Enemy":"Red Locust Bees",
            "SpawnTime":"7:40 AM"
        },
        {
            "Enemy":"GiantKiwi",
            "SpawnTime":"7:40 AM"
        },
        {
            "Enemy":"Red Locust Bees",
            "SpawnTime":"7:40 AM"
        },
        {
            "Enemy":"Red Locust Bees",
            "SpawnTime":"7:40 AM"
        }
    ],
    "NightTimeSpawns":
    [
        {
            "Enemy":"Earth Leviathan",
            "SpawnTime":"7:00 PM"},
        {
            "Enemy":"Earth Leviathan",
            "SpawnTime":"7:00 PM"
        },
        {
            "Enemy":"Baboon hawk",
            "SpawnTime":"7:00 PM"
        },
        {
            "Enemy":"Baboon hawk",
            "SpawnTime":"7:00 PM"
        }
    ]
    "MissedItems":
    [
        {
            "Value":29,
            "ItemType":"Stop sign",
            "DespawnPosition":
            [
                -21.3,
                -225.6,
                51.0
            ],
            "CollectedOnPreviousDay":false
        },
        {
            "Value":38,
            "ItemType":"Large axle",
            "DespawnPosition":
            [
                -20.3,
                -218.9,
                86.2
            ],
            "CollectedOnPreviousDay":false
        },
        ...
    ],
}
```
