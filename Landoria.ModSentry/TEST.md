# ModSentry manual checks

Use Valheim `0.221.12`. Check dedicated-server and peer-hosted sessions.

| Test | Action | Expected result |
| --- | --- | --- |
| Missing ModSentry | Join without ModSentry. | Connection is rejected before admission. |
| Missing inventory | Send peer information without completing verification. | Connection is rejected. |
| Matching plugins | Join with approved required plugins. | Player joins normally. |
| Missing or changed plugin | Remove a required plugin or change its version or contents. | Connection is rejected. |
| Unexpected plugin | Add a DLL outside the required and optional policies. | Connection is rejected. |
| Optional plugin | Join with and without an approved optional plugin. | Both connections are accepted. |
| Server access lists | Join with a banned account or an account outside an enabled permitted list. | Connection is rejected. |
| Reconnect | Disconnect, then reconnect. | A new nonce and inventory are required. |
| Replay | Resend an old inventory on a new connection. | Connection is rejected. |
| Known cheat tool | Enable inspection, then load a recognized tool. | Detection is reported and the server kicks the client. |
