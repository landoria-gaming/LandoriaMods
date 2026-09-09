# RavenWatch

Minimal proof of concept on the `poc` branch.

- Loads as a BepInEx plugin on clients and dedicated servers.
- Writes a startup message to the BepInEx log.
- Dedicated servers log incoming and outgoing RPCs in a JSON array under `data/RavenWatch/rpc-*.json`, with a new file per server session.
- Each entry contains `request` and a `response` array of outgoing calls observed during handling. These are not guaranteed replies; independent outgoing calls have a null request. Delayed replies are not automatically linked.
- Known payloads are decoded using the embedded Valheim 1.0.7 definitions. Unsupported payloads record a decoding error.
- A separate `data/RavenWatch/rpc-errors-*.json` array is created at each dedicated-server startup, even without errors. It records failed decoding attempts with the call ID, direction, peer, RPC hash when available, packet size and full error. These errors also remain in the main journal.
- Clients do not collect events. No cheat detection is active.
- `data/RavenWatch/pickups-*.json` records incoming `RPC_SetPicked(true)` reports, with the target ZDO's prefab name, position, owner, revisions and decoded state as known by the server before handling the RPC. Missing ZDOs are marked explicitly. A new file is created per server session; these reports do not prove inventory changes.
- The same filtered journal also records inferred vanilla item drops: a new `ItemDrop` ZDO, created and owned by the sending client, with `pickedUp: true`. Entries include the player, quantity, item data and full incoming ZDO. Existing ZDO updates are excluded. Items without the picked-up flag are not classified as drops.
- Every filtered event includes `characterId` (the persistent profile player ID, written as a string) and `playerName` for the sending peer's character. Session and character ZDO IDs are included separately. If the player ID is not yet known, `characterId` is null and `characterIdentityStatus` is `not_available`.
- The filtered journal records `first_connection` when a character's ZDO shows the first-spawn introduction, once per character per server session. It includes the character identity, full ZDO and any nearby Valkyrie in the same packet. This infers a new character's first arrival with an unmodified client; it does not track first visits to this server.
- Each `first_connection` includes an `inventory` array with one torch and one equipped rag tunic. Its `inventorySource` is `assumed_starting_inventory`; this inventory is not received from the client.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
