# Journal coverage

RavenWatch keeps only server events and observations made by another client. It does
not collect or accept events in which a player reports their own actions. Client
observations are buffered in memory and queued for RPC transmission every five seconds.
Clients do not write journal files.

## Sources

| Source | Meaning |
|---|---|
| `server` | Evidence collected directly by the dedicated server. |
| `observer` | A client observed a remote player or a network object received from another source. |

The server derives the observer identity from the RPC connection. Observer reports are
still untrusted client evidence and should be correlated with server events or reports
from other observers.

## Server events

Source folder: `Server/EventCollection/`.

| Code | Event | Main details |
|---|---|---|
| `CREATURE_SPAWNED_SERVER` | The server first sees a newly created creature ZDO | Prefab, network ID, sender, position, biome, nearby spawners, natural spawn rules, active raid details and the vanilla `EventCreature` marker. |
| `PLAYER_DEBUG_FLY_SERVER` | A player sends network data with debug fly enabled | Player, network ID, sender, owner and position. |
| `PLAYER_DAMAGED_AT_ONE_HEALTH_SERVER` | A player at 1 health reports positive damage | Player, network ID, health, damage and position. |
| `GROUND_ITEM_SPAWNED_SERVER` | The server receives a newly created ground item | Item data, network sender, owner, position, velocity, nearby player geometry, character first-seen time and visible equipment. |
| `GROUND_ITEM_REMOVED_NEAR_PLAYER_SERVER` | A player-owned ground item is removed near that player | Item data, network ID, player and distance. Used as pickup evidence. |

## Observer events

Source folder: `Client/EventCollection/Observer/`.

| Code | Event | Main details |
|---|---|---|
| `PLAYER_APPEARANCE_OBSERVED` | A remote player becomes loaded | Player identity, network owner, position, direction, velocity and distance. |
| `PLAYER_DEBUG_FLY_OBSERVED` | A remote player enables synchronized debug fly | Player identity, network owner, position, velocity and distance. |
| `CREATURE_APPEARANCE_OBSERVED` | A network-loaded creature appears nearby | Creature prefab, network owner, position, level, health and nearby players. Locally created creatures are excluded. |
| `CONTAINER_OPEN_OBSERVED` | A remote container opening is replicated | Container identity, position and previous/current network owner. Locally owned containers are excluded. |
| `GROUND_ITEM_OBSERVED` | A network-loaded ground item appears | Item, inventory-history marker, network owner, synchronized spawn time, position, velocity and nearby players. Locally created items are excluded. |

## Journal operation

Server journal files use the `.json` extension and contain a JSON array of objects.
`character-history.json` records when each character ID was first seen by this server.
Every cheat detection finding includes two required confidence values from `1` to `3`:
one for the anomaly and one for the attribution to the suspected player. Both final
confidence values are rounded to scores out of `10` using the same weighting. Server
evidence has a weight equal to the number of observer findings, with a minimum weight
of one. A single observer has a weight of `0.5`; two or more observers each have a
weight of `1`.

| Code | Event | Main details |
|---|---|---|
| `JOURNAL_OVERFLOW` | Observer buffer overflow | Number of observations omitted because the bounded memory buffer was full. |

The server rejects all former player-action codes before they enter the ordered memory
buffer. Accepted observer batches are stored with authoritative connection identity,
receipt time and `source: "observer"`.

## Limits

- An observer is still a client and can submit false data. Use observer reports as
  supporting evidence and prefer server evidence for a detection.
- Loading a network object does not establish when or why it was created.
- Network ownership and proximity do not prove who created or used an object.
- Observer events cover loaded objects only. Brief actions and unloaded objects can be missed.
- Debug fly detection covers clients that synchronize Valheim's `DebugFly` flag. A modified
  client that implements flight without this flag requires a separate movement detection.
- Removing an item ZDO near its owner is consistent with a pickup but does not expose the
  player's private inventory contents.
- The character history records first contact with this server, not the character profile's
  creation date.
- Session and network IDs are not Steam or Xbox account IDs.
- Client buffers are bounded. Offline observations, oversized events and queue overflow
  can be lost; there is no disk replay.
