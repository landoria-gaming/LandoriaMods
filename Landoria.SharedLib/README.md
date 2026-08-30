# Plugin SharedLib

`Landoria.SharedLib` contains the common runtime infrastructure used by every Landoria plugin.

## Valheim compatibility

| Valheim channel | Version | Compatibility |
|---|---:|---|
| Current release | `0.221.12` | Compatible |
| Public Test | `0.221.13` | Compatible |

| Component | Purpose |
|---|---|
| `LandoriaPlugin` | Initializes and removes Harmony patches belonging to the concrete plugin namespace. |
| `ModLog` | Routes plugin diagnostics through the BepInEx logger and the debugger output. |
| `ILRepack.targets` | Embeds this library into each standalone plugin DLL. |

The library is a build-time project dependency. Players and server operators do not install a separate `Landoria.SharedLib.dll`; every player-facing Landoria DLL contains the required code through ILRepack.

## Development

The private build automation supplies the shared project reference and ILRepack configuration when packaging the public source. Each standalone plugin embeds its own copy of SharedLib.

## Contact

Report bugs through [GitHub Issues](https://github.com/landoria-gaming/LandoriaMods/issues). For other conversations, use [GitHub Discussions](https://github.com/landoria-gaming/LandoriaMods/discussions).

All Landoria mods are used on the [Landoria Valheim public servers](https://valheim.landoria-gaming.com/).