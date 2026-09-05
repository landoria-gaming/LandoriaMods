# Inventory replay protection

Protocol 2 binds each inventory to a single connection challenge.

1. The server generates 32 cryptographically random bytes when registering the connection.
2. Before sending vanilla peer information, the client requests the challenge using `Landoria_ModSentry_ChallengeRequest_v2`.
3. The server replies through `Landoria_ModSentry_Challenge_v2`. Repeated requests do not replace or extend the challenge.
4. The client sends protocol 2, the Base64 nonce, and its inventory through the existing inventory RPC, then resumes peer information with the original password argument.
5. The server consumes the nonce before parsing the inventory. Missing, incorrect, expired, or unexpected nonces reject verification. The existing DLL version and hash checks still apply.

- Inventory submissions after acceptance or rejection are ignored and cannot change that decision.
- Every connection requires an approved inventory. Clients without ModSentry are rejected.
- Both sides enforce a 30-second deadline once the challenge exchange starts. Connection cleanup and plugin shutdown discard challenge state.
- Protocol 1 inventories are rejected; new clients time out with an update message on servers without challenge support. Client and server need compatible versions.
- The nonce prevents replay of an old packet. It does not prove that a modified client reported its real DLLs.
- Nonces and passwords are never logged.

The client pauses `ZNet.SendPeerInfo(ZRpc, string)` with a prefix while waiting.
Resuming this private vanilla method by reflection is a project-specific exception
limited to this handshake. The signature was checked in the installed game assembly.
The normal patched method is resumed so other plugins can send their own handshake
data before peer information. CharacterVault respects `__runOriginal` to avoid an early duplicate greeting.

Validation: compile both affected mods and inspect the protocol paths. In-game
checks still need a client and server: valid connection, invalid nonce, repeated
inventory, replay after reconnect, timeout, password-protected connection, and rejection of clients without ModSentry.
