# ModSentry security design

## Known managed cheat detection

ModSentry inspects managed assembly metadata on the client for precise
ValheimTooler, ValheimHack223, and valheim-hax markers. It reports a match to
the connected server, which associates the evidence with its source connection
and kicks that client.

This protection is disabled by default. The dedicated server enables it with
`--modsentry-known-cheat-protection true` and requests inspection only after it
has accepted the client's ModSentry inventory.

- Existing assemblies are inspected incrementally to avoid frame stalls.
- Assemblies loaded later are queued through the application-domain load event.
- A match requires the standard assembly marker or type namespace.
- Only the tool, detection vector, and matched marker are sent to the server.
- Process lists, window titles, and unrelated assembly metadata are never sent.
- Detection results cause a kick, never a ban.

## Documented reflection exception

This detector is the project's narrow exception to the general prohibition on
runtime reflection. Injected managed assemblies may have no file on disk, so
their type namespaces must be read from the loaded runtime metadata.

The exception is limited to assembly names and type namespace metadata.
ModSentry does not read fields, invoke methods, alter members, or inspect
unrelated runtime values.

This client-side signal raises the cost of using an unmodified public tool, but
a modified client can still hide or falsify it. Server-side behavior validation
remains authoritative.
