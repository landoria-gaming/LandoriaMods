# RavenWatch

Inventory tracking and RPC logging for Valheim 1.0.7. This proof of concept requires the plugin on the dedicated server and its Steam/Xbox clients. It does not enforce anti-cheat rules.

## How it works

- Clients send lightweight events for item entries, exits, successful crafts and equipped items breaking.
- Every five seconds, the server requests a complete inventory with the last included event sequence.
- The server compares consecutive inventories against those events and determines craft materials from its own recipes.
- Matching events move to the verified journal. Unique event IDs and saved links prevent duplicate use; missing or ambiguous matches stay pending.
- Server pickup/drop observations and craft start/end signals are matched separately, without counting the same item movement twice.

Verification means consistency with client reports, not independent proof. The first inventory establishes a baseline. Pickup observations are inferred from world changes; a new character folder assumes a starting torch and rag tunic. Construction and consumption currently appear as inventory changes, without a dedicated action label. Peer-hosted games do not run server verification or write these journals.

## Files

Files are stored under `RavenWatch/` in the server's Valheim save directory. Each character has a `<platform_accountId>_<playerName>/` subfolder.

| File | Contents |
|---|---|
| `rpc-*.json` | Optional full RPC journal for one server session. |
| `rpc-errors-*.json` | RPC decoding failures. |
| `<character>/inventory.last.json` | Latest inventory received from the client. |
| `<character>/inventory.previous.json` | Previous inventory, replacing the older backup. |
| `<character>/inventory-events-unverified.json` | Events awaiting verification, preserved across restarts. |
| `<character>/inventory-events-verified.json` | Verified events, inventories and their matching links. |

Verification reads inventories and events from the event journals. The two standalone inventory files only retain the latest snapshots. Inventory events have unique IDs and UTC `eventDate` values; the unverified journal has a fixed filename. Old dated journals are merged into it on first access, without duplicate event IDs. RPC records group outgoing calls observed during handling, which are not necessarily replies.

`RpcCapture.LogRpcTraffic` controls the full RPC journal and defaults to `false`. Set it to `true` and rebuild to enable `rpc-*.json`. RPC decoding and `rpc-errors-*.json` remain active in both modes; inventory journals are unaffected. Existing files are kept.

## Source layout

| Location | Responsibility |
|---|---|
| `Client/` | Player hooks, event sending and inventory replies. |
| `Server/` | Dedicated-server hooks, requests, verification and journals. |
| `Shared/` | Protocol, serialization, item comparisons and logger. |
| `Resources/` | Embedded RPC definitions. |
| [RavenWatch Tool](../Landoria.RavenWatchTool/README.md) | Separate offline definition generator. |

Client and dedicated-server patches are installed separately when Valheim networking starts.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
