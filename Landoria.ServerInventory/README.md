# ServerInventory

Moves gameplay authority to a dedicated server. Install on **server and clients**.

| Feature | Server implementation |
| --- | --- |
| Character saves | Custom JSON storage per account and character, beside the world in `characters_local` |
| Combat and object damage | Native Valheim functions with adapted execution and synchronization |
| Harvesting, processing and tombstones | Native functions with server inventory context |
| Inventory transfers, crafting, upgrades, trading and building | Rebuilt workflows using native functions and server checks |
| Object creation and world updates | Server creation; custom filtering of client requests and updates |

- New characters keep their appearance and start with a torch, hammer and stone axe. Existing local saves are not updated, including in solo games.
- **Experimental:** compiled, not validated in multiplayer. Movement and discovery reports still rely on clients; healing and several gameplay paths remain incomplete.
- [Report a bug](https://github.com/landoria-gaming/LandoriaMods/issues)
