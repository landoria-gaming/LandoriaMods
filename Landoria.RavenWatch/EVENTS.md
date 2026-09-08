# Collected Events

RavenWatch keeps only server events and observations made by another client. It does
not collect or accept events in which a player reports their own actions. Client
observations are buffered in memory and queued for RPC transmission every five seconds.
Clients do not write journal files.

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
