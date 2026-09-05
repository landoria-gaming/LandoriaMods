# CharacterVault guest integration

ModSentry optionally patches CharacterVault to bypass character validation and
final saves for admitted temporary guests.

| Contract | Value |
|---|---|
| Target type | `Landoria.CharacterVault.ProfileTransferService` |
| Target method | Static `bool ShouldStoreCharacterOnServer(ZRpc rpc)` |
| Default result | `true`: validate and store the character on the server |
| ModSentry postfix | Sets the result to `false` for connections in `GuestAdmissions` |

- The soft BepInEx dependency loads CharacterVault first when installed; neither mod requires the other.
- This integration intentionally uses reflection to resolve the optional target without a build dependency. This is a project-specific exception for this patch only.
- The target disables inlining so the patch applies to every call.
- Admission and kick-save decisions use the same method.
- A missing CharacterVault plugin skips the patch. An incompatible method logs a warning.
- The patch uses ModSentry's Harmony ID and is removed by its normal shutdown cleanup.
- No callback, shared interface, or CharacterVault DLL is bundled with ModSentry.
