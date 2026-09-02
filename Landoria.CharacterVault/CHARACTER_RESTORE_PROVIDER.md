# Character restore provider integration

A server-side mod can provide a CharacterVault-verified character when that character is not yet stored on the current server. This allows the same verified character to be used across several worlds.

CharacterVault calls the provider only when the character is missing from the server's local CharacterVault storage. It never replaces an existing local profile.

## Requirements

- Add `Landoria.CharacterVault` as a hard BepInEx dependency.
- Reference `Landoria.CharacterVault.dll` without copying it into your mod package.
- Implement `ICharacterRestoreProvider` in the server-side mod.
- Register one provider when the mod starts and unregister it when the mod stops.
- Return only profiles for the requested platform player and character name, after applying the provider's own compatibility rules.

## Provider example

```csharp
using System.Threading;
using System.Threading.Tasks;
using Landoria.CharacterVault;

internal sealed class MyCharacterRestoreProvider : ICharacterRestoreProvider
{
    public async Task<CharacterRestoreResult> RestoreAsync(
        string platformPlayerId,
        string playerName,
        CancellationToken cancellationToken)
    {
        // Find, download, and validate the latest eligible profile, then return Restored, NotFound, or Failed.
    }
}
```

Return values:

| Result | Meaning |
|---|---|
| `CharacterRestoreResult.Restored(profile)` | A valid profile was found and can be admitted. |
| `CharacterRestoreResult.NotFound()` | No compatible profile exists; CharacterVault applies its normal new-character rules. |
| `CharacterRestoreResult.Failed()` | The provider could not safely complete the lookup; CharacterVault rejects the connection temporarily. |

## Registration example

```csharp
using BepInEx;
using Landoria.CharacterVault;

[BepInPlugin(PluginGuid, PluginName, PluginVersion)]
[BepInDependency("Landoria.CharacterVault", BepInDependency.DependencyFlags.HardDependency)]
public sealed class MyServerPlugin : BaseUnityPlugin
{
    private MyCharacterRestoreProvider _restoreProvider;

    private void Awake()
    {
        // Create the provider from the server configuration and register it with CharacterVault.
    }

    private void OnDestroy()
    {
        // Unregister and dispose the provider if it owns disposable resources.
    }
}
```

Only one provider may be registered in a server process. `CharacterRestoreApi.Register` throws if another provider is already registered.
