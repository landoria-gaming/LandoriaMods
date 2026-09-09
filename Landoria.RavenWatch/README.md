# RavenWatch

Minimal proof of concept on the `poc` branch.

- Loads as a BepInEx plugin on clients and dedicated servers.
- Writes a startup message to the BepInEx log.
- Dedicated servers log incoming and outgoing RPCs in a JSON array under `data/RavenWatch/rpc-*.json`, with a new file per server session.
- Each entry contains `request` and a `response` array of outgoing calls observed during handling. These are not guaranteed replies; independent outgoing calls have a null request. Delayed replies are not automatically linked.
- Known payloads are decoded using the embedded Valheim 1.0.7 definitions. Unsupported payloads record a decoding error.
- A separate `data/RavenWatch/rpc-errors-*.json` array is created at each dedicated-server startup, even without errors. It records failed decoding attempts with the call ID, direction, peer, RPC hash when available, packet size and full error. These errors also remain in the main journal.
- Clients do not collect events. No cheat detection is active.
- `data/RavenWatch/<platform_accountId>_<playerName>/inventory-events-YYYYMMDD.json` records flat inventory changes in arrival order (for example `Steam_123456789_Nini`). The date follows the server's local time. Each day gets its own file, appended across restarts; rotation happens automatically on the next event. Invalid filename characters are replaced with underscores. Older journals are preserved without migration.
- Fields: `eventId` (a new GUID per event), `utc`, `event`, `characterId`, `playerName`, `prefabHash`, `prefabName`, `quality`, `variant`, `worldLevel`, `quantityDelta`, `x`, `y`, `z`, `biome`. Pickups add quantities; drops subtract them. Character IDs identify saved profiles, not network sessions.
- Coordinates use the incoming object position for drops and the last server ZDO position for pickups. Starting items use the character position. `biome` is the vanilla biome at those coordinates; unavailable location data is null. Existing entries are preserved without backfilling.
- Each event also includes `weight` (unit weight, adjusted for quality), `stackable` and `maxStackSize`, read from the server's item prefab. These fields are null if the prefab is unavailable.
- Client-requested ItemDrop destruction counts as a pickup under the current assumption. Pickable steps are excluded to avoid counting the items they produce twice. Missing item data cannot produce a quantity entry.
- Newly created client-owned items count as drops when marked as previously picked up, or when matching the starting torch or rag tunic. Starting equipment keeps `pickedUp: false` until first picked up from the ground.
- Inventory events come directly from server Harmony patches: `RPC_ZDOData` establishes the sending peer, `CreateNewZDO` identifies new objects, `ZDO.Deserialize` provides their accepted state, and `RPC_DestroyZDO` captures items before removal. Item data uses the vanilla reader; the inventory journal does not depend on RPC JSON decoding.
- A character folder without any inventory journal is treated as new: two `pickup` entries add one torch and one rag tunic before subsequent events. Initialization happens when the character is identified, even without the introduction. Previous daily files and the legacy `_inventory-events.json` prevent reseeding at midnight or after a restart.
- Tombstone withdrawals produce `pickup` events attributed to the client sending the accepted ZDO update. Before/after quantities are compared by prefab, quality, variant and world level; stack rearrangements do not count as pickups. The first observed content is only a baseline, and event coordinates come from the tombstone.
- This estimates item quantities, not a complete inventory: crafting, consumption, death losses, other container transfers and changes outside this server are not tracked.
- Each character folder also contains `inventory.json`, refreshed after every event and rebuilt from all its event journals when opened. It contains remaining pickup events with their original IDs and metadata. Drops consume the oldest matching character/prefab/quality/variant/world-level entries; partial drops reduce `quantityDelta`, and exhausted entries disappear. The event journals remain unchanged. Unmatched drop quantities are logged without creating negative stock.
- Unmatched drops are appended to `inventory-drop-errors.json` in the same folder, including the original event and `unmatchedQuantity` (also for partially matched drops). Event IDs prevent duplicate errors when rebuilding inventory after a restart; older events without IDs use a content fingerprint.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
