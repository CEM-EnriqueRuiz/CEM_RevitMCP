---
title: "0016 The TS unit suite tests zod schemas only"
type: decision
status: current
updated: 2026-10-06
sources: ["https://github.com/CEM-EnriqueRuiz/CEM_API/issues/50", 0556fb0, server/tests/TESTS.md, server/tests/helpers/captureToolSchema.ts]
related: [../workflows/test.md, ../sources/issues/CEM-EnriqueRuiz-CEM_API-50.md]
tags: [testing, tdd]
---

# 0016 The TS unit suite tests zod schemas only

## Context

The cross-repo TDD effort ([CEM_API#50](../sources/issues/CEM-EnriqueRuiz-CEM_API-50.md)) gave
every repo a runnable unit suite. Before it, `server/` had no test runner. Almost every TS tool is a
thin wrapper: it builds a params object and calls
`withRevitConnection(c => c.sendCommand(...))`, which opens a real TCP socket. `SocketClient`'s
buffering is entangled with live socket state.

## Decision

The vitest suite (`server/tests/`, run with `npx vitest run` or `npm test`) covers only the one
genuinely pure surface: **each tool's zod schema**. `tests/helpers/captureToolSchema.ts` stubs
`McpServer.tool(...)`, imports the *real* `register*` export, and rebuilds the exact shipped
`z.object(shape)`. Each test file asserts that a minimal valid payload parses, a fully specified
valid payload parses, and each documented invalid shape fails `.safeParse()`.

There is one representative tool per domain: `create_level` (Core), `family_add_parameter`,
`arch_create_wall`, `mep_create_duct`, `viz_create_material`.

**Rejected:** mocking the socket. That mock would be heavier than the code under test.
`withRevitConnection` and the socket stay a documented gap.

## Consequences

- A new tool's schema test is written before or alongside the schema. Copy the shape of an
  existing test and add a line to `server/tests/TESTS.md`.
- The C# `tests/commandset/` suite (`Nice3point.TUnit.Revit`, upstream `ea54485`, 40 tests) needs
  a live, licensed Revit. It is a separate tier and is not part of the fast loop.
- Driving Revit through MCP while developing is not a test (root TDD reference).
