# Server authority

The primary goal of this mod is to prevent a modified client from imposing gameplay results. Server authority takes priority over synchronizing client-reported state.

- Clients request actions, such as picking up an object, crafting an item or attacking a target. They must not dictate the resulting inventory, damage, health, resources or progression.
- The server validates each action against its own authoritative state, executes it, persists the relevant changes and sends the result to clients. Authenticate the sender from the connection; do not trust identities supplied in the request.
- Reuse native Valheim gameplay functions on the dedicated server wherever possible. Adapt their execution context rather than duplicating or rewriting their rules unnecessarily.
- Neutralize the corresponding client-side gameplay execution. Client input, menus, rendering, animation and cosmetic prediction may remain local, but must not authorize gameplay effects.
- An unvalidated object or action must not affect shared gameplay, cause damage or be broadcast to other clients as accepted state. Enforce this on the server; client patches alone do not protect against a modified client.
- Treat existing client-reported inventory, healing, discoveries and other results as unresolved authority gaps, not as trusted server validation. Do not extend these patterns as the final implementation.
- For each change, identify what remains client-controlled and which server checks prevent forged requests. Distinguish native code moved to the server, rebuilt workflows and custom code. Do not claim cheat resistance from successful compilation or synchronization alone.
