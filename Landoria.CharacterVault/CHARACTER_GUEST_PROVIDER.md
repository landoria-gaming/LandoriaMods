# Character guest provider integration

A server integration can identify temporary guests through `ICharacterGuestProvider`.
CharacterVault does not validate, import, or save their character profiles.

| Situation | CharacterVault behavior |
|---|---|
| No provider or a null RPC | Treat the connection as a regular player. |
| `IsGuest(rpc)` returns `false` | Apply normal character validation and save rules. |
| `IsGuest(rpc)` returns `true` | Admit the guest without a vault profile and allow disconnection without saving. |

- Reference `Landoria.CharacterVault.dll` without bundling a copy.
- Declare a hard BepInEx dependency on `Landoria.CharacterVault` in the provider mod.
- Implement `bool IsGuest(ZRpc rpc)` using trusted server-side session state.
- Register one provider with `CharacterGuestApi.Register(provider)` when the integration starts.
- Unregister that same instance with `CharacterGuestApi.Unregister(provider)` when it stops.
- Establish guest admission before CharacterVault handles `ZNet.RPC_PeerInfo`.
- Keep the lookup synchronous and lightweight. CharacterVault queries the provider for admission and server kicks; it does not cache the result.
- Registering another provider while one is active throws an exception. Unregistering a different instance leaves the active provider intact.

Only trusted temporary sessions should return `true`: this bypasses character validation.

ModSentry implements this contract using its in-memory guest admission registry.
It registers its provider at startup and unregisters the same instance when it stops.
CharacterVault does not read ModSentry markers or reference its assembly.
