# RavenWatch

Inventory tracking for Valheim 1.0.7. Install on the dedicated server and its Steam/Xbox clients.

## How it works

- The client reports items picked up, removed, crafted or broken.
- Every five seconds, the server requests the full inventory and checks which events explain the changes. Craft materials come from the server's recipes.
- Matching events are marked as verified. Others stay pending and move to unverified after three failed attempts, counted once per new inventory.
- When an item event becomes unverified, all connected players receive a chat message.
- An unexplained change does not block unrelated items. Dates are informational; unique event IDs prevent duplicate use.

Only the server writes inventory journals. Verification checks consistency with client reports; it does not prove that items were obtained legitimately. Recording an inventory does not mean every change is explained. Peer-hosted games are not supported.

## Files

Stored in `RavenWatch/` under the server's Valheim save directory, with one folder per character.

| Character file | Contents |
|---|---|
| `inventory.last.json` | Latest inventory. |
| `inventory.previous.json` | Previous inventory. |
| `inventory-events-pending.json` | Events awaiting a match, with attempt counters. |
| `inventory-events-verified.json` | Matched events and recorded inventories. |
| `inventory-events-unverified.json` | Events unmatched after three attempts. |

Checks use the event journals. Pending events and counters survive restarts.

## Client command

`toggleRavenWatch` toggles client event sending and prints its current state. Full inventory replies remain active. Events skipped while disabled are not sent later. Sending is enabled when the game starts.

## RPC logs

- RPC capture and decoding are disabled by default (`RpcCapture.EnableRpcCapture = false`). Inventory tracking and chat alerts remain active.
- Set `EnableRpcCapture` to `true` and rebuild to inspect RPCs. The first decoding error creates `rpc-errors-*.json`.
- Also set `RpcCapture.LogRpcTraffic` to `true` to write full `rpc-*.json` logs.

Code is split into `Client/`, `Server/` and `Shared/`. [RavenWatch Tool](../Landoria.RavenWatchTool/README.md) generates RPC definitions separately.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
