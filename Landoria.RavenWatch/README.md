# RavenWatch

A server anti-cheat that uses observations from other clients and the server to detect unauthorized actions performed by a modified client.

## Features

- Clients send only observations of other players and network objects.
- The server records its own events and cross-checks them with events received from other clients to detect impossible actions performed by a client.
- RavenWatch only reports suspected cheating in the chat for all connected clients. Another server mod can use the cheat report hook to implement a response such as an automatic kick.
- Server journals are stored in the Valheim data directory under `RavenWatch`.

## Cheat detection

- Suspicious creature spawns are reported when no vanilla spawn, raid or nearby spawner can explain them.

## Integration

Server mods can subscribe to `RavenWatchApi.CheatReported`. The report provides the
suspected session, final confidence score, explanation and complete detailed JSON, so
the subscriber can apply its own action such as an automatic kick. Setting
`report.SuppressChat = true` prevents the player chat alert while retaining the server
log and cheat detection journal.

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/X5J1wSRr7Uo)

| Crossplay support | Steam network support |
|---|---|
| Yes | No |

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |

Install matching versions of RavenWatch on the server and every participating client.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
