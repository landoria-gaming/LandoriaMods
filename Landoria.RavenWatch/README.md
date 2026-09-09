# RavenWatch

Minimal proof of concept on the `poc` branch.

- Loads as a BepInEx plugin on clients and dedicated servers.
- Writes a startup message to the BepInEx log.
- Dedicated servers log incoming and outgoing RPCs in a JSON array under `data/RavenWatch/rpc-*.json`, with a new file per server session.
- Each entry contains `request` and a `response` array of outgoing calls observed during handling. These are not guaranteed replies; independent outgoing calls have a null request. Delayed replies are not automatically linked.
- Known payloads are decoded using the embedded Valheim 1.0.7 definitions. Unsupported payloads record a decoding error.
- A separate `data/RavenWatch/rpc-errors-*.json` array is created at each dedicated-server startup, even without errors. It records failed decoding attempts with the call ID, direction, peer, RPC hash when available, packet size and full error. These errors also remain in the main journal.
- Clients do not collect events. No cheat detection is active.
- `data/RavenWatch/inventory-events-*.json` records flat inventory changes in arrival order, with a new file per server session.
- Fields: `utc`, `event`, `characterId`, `playerName`, `prefabHash`, `prefabName`, `quality`, `variant`, `worldLevel`, `quantityDelta`. Pickups add quantities; drops subtract them. Character IDs identify saved profiles, not network sessions.
- Client-requested ItemDrop destruction counts as a pickup under the current assumption. Pickable steps are excluded to avoid counting the items they produce twice. Missing item data cannot produce a quantity entry.
- A detected first-spawn introduction produces two flat `first_connection` entries: one starting torch and one rag tunic, each with `quantityDelta: 1`. This assumes starting equipment; skipped introductions are not detected.
- This estimates item quantities, not a complete inventory: crafting, consumption, container transfers and changes outside this server are not tracked.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
