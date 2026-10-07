---
title: "0012 Drop the TS-only SQLite store pack and unused upstream TS tools"
type: decision
status: current
updated: 2026-10-06
sources: [aa88b96, server/package.json, CEM_RevitMCP.md]
related: [0015-single-file-bundle.md, ../concepts/doc-drift.md, ../modules/server.md]
tags: [server, removal, upstream-divergence]
---

# 0012 Drop the TS-only SQLite store pack and unused upstream TS tools

## Context

Upstream's `server/` had a TS-only local SQLite pack: `store_project_data`, `store_room_data` and
`query_stored_data`, backed by `server/src/database/db.ts` and `service.ts` and the native
`better-sqlite3` dependency. It never touched Revit. Upstream also had TS tools without a matching
C# command, or under other names: `color_elements`, `modify_element`, `search_modules`,
`use_module`, `tag_all_walls` and `tag_all_rooms`.

## Decision

In `aa88b96` (2026-06-15), all of these were removed:

- The SQLite pack and `src/database/` were dropped together with `better-sqlite3`, so the server
  bundles into a single cross-platform file ([0015](0015-single-file-bundle.md)). The cache was a
  per-machine scratchpad that nothing else used.
- `color_elements` was replaced by `color_splash` (the TS tool name must equal the `command.json`
  name).
- `tag_all_walls` and `tag_all_rooms` were removed in favour of `tag_elements` with categories.
- `modify_element`, `search_modules` and `use_module` were removed. The reason is not recorded.

## Consequences

- **Superseded claim:** the old skill (`b05a76f`) said the SQLite pack was "deliberately kept —
  don't fix them by adding a C# side". That contradicted the code from the day it was written. It
  was flagged in `server/tests/TESTS.md` (`0556fb0`) and is corrected here. The code wins.
- The C# commands `tag_walls` and `tag_rooms` (upstream) are **still registered** (deliberately; consolidated into `tag_elements`, `29b3fba`) in `command.json`
  and the command set, but now have **no TS tool**, so MCP clients cannot reach them. See
  [doc drift](../concepts/doc-drift.md).
- A gitignored `server/revit-data.db` may still exist on old checkouts; it is a leftover.
