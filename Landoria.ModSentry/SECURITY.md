# ModSentry security design

| Check | Purpose |
|---|---|
| Client mod inventory | Compare mod identifiers, versions, and SHA-256 hashes with the server policy. |
| Connection nonce | Require a fresh inventory response for each connection. |
| Admission | Reject clients whose inventory is missing or does not match the policy. |

## Limits

- The client supplies its inventory; a modified client can falsify it.
- ModSentry does not inspect loaded assemblies for cheats or injected code.
- ModSentry does not issue cheat reports or apply cheat-related kicks or bans.
- Server access lists and mod policy rejections still apply.
