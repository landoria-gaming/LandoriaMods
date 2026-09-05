# Socialize

Adds player groups for adventures and expeditions, private messaging, map sharing, and a dedicated group chat channel.
Groups are not persistent: players leave them when they disconnect and do not rejoin automatically when they return.

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

## Features

### Groups

- Lets up to five connected players form a group for their current session.
- Gives group leaders invite, remove, and leadership transfer controls.
- Shows group invitations through Valheim's Yes/No popup.
- Automatically promotes the longest-standing remaining member when the group leader leaves.
- Disbands a group when fewer than two members remain connected.
- Removes disconnected players from their group and does not restore groups after reconnection or a server restart.

### Chat

- Removes the public server-wide chat channel: normal chat reaches nearby players, while shouts travel farther within server-defined ranges.
- Adds private group chat for connected group members.
- Adds world-wide private messages and optional animated pings for a chosen player.
- Confirms private messages, targeted pings, group messages, and invitations only after delivery.
- Applies platform text permissions and filtering before displaying custom messages.
- Keeps the selected normal, shout, group, or private chat channel active for following messages.
- Keeps private messages inside the chat window instead of displaying them above the character.

### Map privacy

- Keeps player positions private outside groups.
- Shares connected group members' positions automatically every two seconds.
- Shares map pings only with group members and disables them outside groups.

### Notifications

- Replaces the local arrival shout with a server-wide localized arrival announcement, and announces when members leave, are removed, become leader, or cause a group to disband.

## Chat Commands

| Command | Purpose |
|---|---|
| `/s <message>` or `/say <message>` | Sends nearby chat. |
| `/sh <message>` or `/shout <message>` | Shouts within the configured local range. |
| `/w <PlayerName> <message>` | Sends a world-wide private message. |
| `/wping <PlayerName> <message>` | Sends a private message and animated ping. |
| `/g <message>` | Messages connected group members. |

## Group Commands

| Command | Purpose |
|---|---|
| `/group help` | Lists group commands. |
| `/group invite <PlayerName>` | Invites a connected player. |
| `/group leave` | Leaves the group. |
| `/group remove <PlayerName>` | Removes a member; leader only. |
| `/group promote <PlayerName>` | Transfers leadership. |
| `/group info` | Lists group members and status. |

## Installation

Most of Landoria mods need to be installed also on the dedicated server, we show you in this video how to do it on windows:

[Setup a Valheim Modded dedicated server on Windows](https://youtu.be/X5J1wSRr7Uo)

| Crossplay support | Steam network support |
|---|---
| Yes | No

| Client required | Server required (dedicated) | Player-hosted server |
|---|---|---|
| Yes | Yes | Not Supported |


## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues).
For questions, feedback, and other discussions, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).
