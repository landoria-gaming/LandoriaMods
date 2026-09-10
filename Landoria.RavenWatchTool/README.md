# RavenWatch Tool

Offline generator for RavenWatch's Valheim 1.0.7 RPC definitions. This command-line project is separate from the BepInEx plugin.

## Usage

Requires the .NET 10 SDK and decompiled dedicated-server sources. Run from the repository root:

```powershell
dotnet run --project ./Landoria.RavenWatchTool/Landoria.RavenWatchTool.csproj -- --source 'C:/src/landoria/RESOURCES/Valheim/1.0.7/dedicated server'
```

| Option | Purpose |
|---|---|
| `--source` | Required: decompiled dedicated-server source directory. |
| `--output` | Optional output path; defaults to `Landoria.RavenWatch/Resources/valheim-1.0.7-rpc.json`, relative to the working directory. |

- Scans RPC registrations and applies reviewed serialization rules, including known ZDO binary data.
- Stops on unresolved registrations or missing layouts. Review the rules when updating Valheim.
- Rebuild RavenWatch after generation to embed the updated JSON. Nothing is generated at game startup.
