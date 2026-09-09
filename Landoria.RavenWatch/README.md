# RavenWatch

Minimal proof of concept on the `poc` branch.

- Loads as a BepInEx plugin on clients and dedicated servers.
- Writes a startup message to the BepInEx log.
- Dedicated servers log incoming and outgoing RPCs in a JSON array under `data/RavenWatch/rpc-*.json`, with a new file per server session.
- Each entry contains `request` and a `response` array of outgoing calls observed during handling. These are not guaranteed replies; independent outgoing calls have a null request. Delayed replies are not automatically linked.
- Known payloads are decoded using the embedded Valheim 1.0.7 definitions. Unsupported payloads record a decoding error.
- A separate `data/RavenWatch/rpc-errors-*.json` array is created at each dedicated-server startup, even without errors. It records failed decoding attempts with the call ID, direction, peer, RPC hash when available, packet size and full error. These errors also remain in the main journal.
- Clients do not collect events. No cheat detection is active.
- `data/RavenWatch/<platform_accountId>_<playerName>/_inventory-events.json` records flat inventory changes in arrival order (for example `Steam_123456789_Nini`). Existing arrays are reopened and appended across server restarts. Invalid filename characters are replaced with underscores. Older session journals are not migrated.
- Fields: `utc`, `event`, `characterId`, `playerName`, `prefabHash`, `prefabName`, `quality`, `variant`, `worldLevel`, `quantityDelta`, `x`, `y`, `z`, `biome`. Pickups add quantities; drops subtract them. Character IDs identify saved profiles, not network sessions.
- Coordinates use the incoming object position for drops and the last server ZDO position for pickups. Starting items use the character position. `biome` is the vanilla biome at those coordinates; unavailable location data is null. Existing entries are preserved without backfilling.
- Client-requested ItemDrop destruction counts as a pickup under the current assumption. Pickable steps are excluded to avoid counting the items they produce twice. Missing item data cannot produce a quantity entry.
- A missing character journal is treated as a new character: two `pickup` entries add one torch and one rag tunic before subsequent events. Initialization happens when the character is identified, even without the introduction. Existing files are never seeded again, including after a restart; deleting the file or changing the character name causes fresh initialization on its next opening.
- This estimates item quantities, not a complete inventory: crafting, consumption, container transfers and changes outside this server are not tracked.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
