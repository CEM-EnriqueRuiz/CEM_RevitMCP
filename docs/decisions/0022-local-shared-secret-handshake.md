---
title: "0022 Local shared-secret handshake for the Revit socket (proposed)"
type: decision
status: proposed
updated: 2026-10-06
sources: [plugin/Core/SocketService.cs, server/src/utils/SocketClient.ts]
related: [0021-loopback-only-socket.md, 0014-license-gated-listener.md]
tags: [security, socket, follow-up]
---

# 0022 Local shared-secret handshake for the Revit socket (proposed)

**Status: proposed.** Recommended, but not implemented. The coordinator asked for it to be recorded
as a follow-up (2026-10-06).

## Context

After [0021](0021-loopback-only-socket.md), the socket accepts only loopback connections but still
has no authentication. The residual exposure:

- **Other users on the same machine** (RDS/terminal servers, shared workstations) can open
  `127.0.0.1:8080` and run `send_code_to_revit` as the Revit user. This is the real gap.
- Any process running as the same user can do the same, but it can already run code as that user,
  so a secret adds little there.
- Browsers cannot practically exploit it. They can't speak raw TCP, and an HTTP request's headers
  make the plugin's `JsonConvert.DeserializeObject` fail.

## Proposal

When the listener starts, the plugin writes a random token to a per-user file (for example
`%LOCALAPPDATA%\Cemengal\RevitMCP\token`, ACL'd to the user). The TS server reads it and sends it
with each request (a JSON-RPC param or a first-line handshake). The plugin rejects requests without
it. Both ends are ours, so no upstream compatibility is needed.

## Open points

The wire format, whether to rotate the token per session, how the MCP server finds the file when it
runs under a different user, and the effect on the 120 s client timeout path.
