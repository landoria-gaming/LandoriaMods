# RPC definitions

Generate the resource from Valheim 1.0.7 dedicated-server sources with the .NET 10 SDK:

```powershell
dotnet run --project .\Tools\RpcDefinitions.csproj -- --source 'C:\src\landoria\RESOURCES\Valheim\1.0.7\dedicated server'
```

- Run from `Landoria.RavenWatch`. The default output is `Resources/valheim-1.0.7-rpc.json`; use `--output` for another path.
- The C# generator scans the dedicated-server sources, including subdirectories. Client sources are not required.
- Reviewed binary rules are built in memory by `WireLayouts` and `BinaryLayouts`. Generation needs no previous JSON, temporary layouts file or third-party NuGet package.
- Rules describe field order, conditions, nested packages, compression and known ZDO binary values. Only the current game version is supported. When Valheim updates, replace these rules after reviewing its serialization code.
- Registrations include literal names and names stored in string constants. Unresolved names or missing package layouts stop generation.
- Responses are conditional outbound candidates, not automatically correlated replies.
- Unknown RPCs, unknown ZDO binary keys (including indexed item keys) and unsupported stored item versions produce explicit decoding errors. Item and inventory binary layouts currently support version 109.
- Generation stays offline. Rebuild the mod to embed the new resource; nothing is generated at server startup.
