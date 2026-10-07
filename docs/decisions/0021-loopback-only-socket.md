---
title: "0021 Loopback-only socket, pinned client host, and audited dispatch"
type: decision
status: current
updated: 2026-10-06
sources: [plugin/Core/SocketService.cs, server/src/utils/ConnectionManager.ts, server/tests/utils/connection.test.ts, plugin/Core/CommandExecutor.cs]
related: [0014-license-gated-listener.md, 0013-hardcoded-failsoft-audit-trails.md, 0022-local-shared-secret-handshake.md, ../concepts/threading-and-external-events.md, ../concepts/upstream-and-fork.md]
tags: [security, socket, audit, divergence]
---

# 0021 Loopback-only socket, pinned client host, and audited dispatch

**CEM divergence from upstream** (`mcp-servers-for-revit` binds `IPAddress.Any` and dispatches
directly). Drafted on 2026-10-06 and committed on 2026-10-07 as a HOT FIX with no issue link
([2026-10 commits](../sources/commits/2026-10.md)).

## Context

- `SocketService.Start` bound `TcpListener(IPAddress.Any, 8080)`. While CEM_RevitAI was on, **any
  machine on the network** could send JSON-RPC to Revit. That includes `send_code_to_revit`
  (arbitrary C# through Roslyn), which amounts to remote code execution on the user's PC. The
  socket has **no authentication** of any kind. The license gate
  ([0014](0014-license-gated-listener.md)) controls whether the listener starts, not who may
  connect.
- The TS client connected to `"localhost"`. Node ≥ 17 resolves `localhost` in OS order (often
  `::1` first), and only Node 20+'s default `autoSelectFamily` fallback reaches an IPv4-only
  listener. That makes the result depend on the Node version and options.
- `SocketService.ProcessJsonRPCRequest` called `command.Execute` itself and **never used
  `CommandExecutor`**, so `ActionLogger` never wrote the `Log\` audit trail, despite
  [0013](0013-hardcoded-failsoft-audit-trails.md) (an upstream path that the Cemengal audit hook did
  not cover).

## Decision

1. **Bind IPv4 loopback only:** `SocketService.BindAddress = IPAddress.Loopback` (`127.0.0.1`).
2. **Pin the client:** `ConnectionManager.ts` exports `REVIT_HOST = "127.0.0.1"` and
   `REVIT_PORT = 8080` and connects to them.
   *Rejected:* binding both `127.0.0.1` and `::1`. That means two listeners and more code, and it
   only helps foreign clients that use `localhost`. Both ends are ours, so one IPv4 listener plus
   an explicit client host is deterministic on every Node version and OS.
3. **Dispatch through `CommandExecutor.ExecuteCommand`.** `SocketService` keeps only JSON parsing
   and validation, so every call, including `send_code_to_revit`, reaches the audit log. Side
   effect: `CommandExecutionException` error codes and data are now preserved in the JSON-RPC error
   instead of being flattened to InternalError. This works together with
   [0023](0023-send-code-scripts-not-persisted.md) (`fe06b6c`): **every command is audited, but no
   script body is logged.** `send_code_to_revit` calls appear in the log with `code`/`data` as
   `"<omitted>"`, and there is no `Recipes\` dump. Checked after rebasing onto `fe06b6c` on
   2026-10-07.
4. **Shared-secret handshake: proposed, not implemented** → [0022](0022-local-shared-secret-handshake.md).

## Consequences

- Remote hosts can no longer connect. Local processes still can (see 0022 for the residual risk).
- The old npm/upstream TS server using `localhost` still works on Node 20+ (its happy-eyeballs
  fallback reaches 127.0.0.1), but don't rely on it.
- Tests: `server/tests/utils/connection.test.ts` asserts the pinned host and port, and drives
  `withRevitConnection` against a fake listener bound only to `127.0.0.1:8080` (skipped if 8080 is
  busy). A mutation to `"localhost"` fails the host test.
  **No C# unit test:** the plugin has no Revit-free test project, and `tests/commandset` (TUnit) runs
  only inside live Revit and references neither the plugin nor the command set. The bind address
  and the audit routing are verified by build plus a live check.
- **Live check (needs Revit open):** start CEM_RevitAI, then `netstat -ano | findstr :8080` must show
  `127.0.0.1:8080 … LISTENING` (not `0.0.0.0:8080`). Call any tool, and a line must appear in
  `Log\actions_<date>.jsonl`.
- Ship it by rebuilding and redeploying CEM_RibbonUI (no code change there).
