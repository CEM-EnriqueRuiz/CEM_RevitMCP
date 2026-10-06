---
title: "0015 Bundle the server into one self-contained file"
type: decision
status: current
updated: 2026-10-06
sources: [aa88b96, server/scripts/bundle.mjs, server/scripts/generate-tool-registry.mjs, server/src/index.bundle.ts]
related: [../modules/server.md, 0012-drop-sqlite-store-pack.md]
tags: [server, build, distribution]
---

# 0015 Bundle the server into one self-contained file

## Context

The first Cemengal batch (`b04b41e`) shipped a committed `cem-revit-ai.mcpb` (6.8 MB) built from
`server/manifest.json`. `register.ts` discovers tools at runtime (`readdirSync` + dynamic
`import()`), which esbuild cannot follow.

## Decision

In `aa88b96`:

- `npm run bundle` first runs `scripts/generate-tool-registry.mjs`, which writes
  `src/tools/register.generated.ts` with **static** imports of every tool (gitignored and recreated
  on every bundle). It then runs `scripts/bundle.mjs`, which uses esbuild with entry
  `src/index.bundle.ts` to produce `dist/index.js`: one ESM file with everything inlined and no
  native dependencies.
- The dev/tsc flow (`npm run build` → `build/index.js`) keeps the runtime directory scan.
- The committed `.mcpb` was deleted from the repo.
- Dropping `better-sqlite3` ([0012](0012-drop-sqlite-store-pack.md)) is what made a single
  cross-platform file possible.

## Consequences

- Two entry points must stay equivalent: `src/index.ts` and `src/index.bundle.ts`.
- **Gotcha:** `register.ts` excludes only `index.*` and `register.*` from its scan, not
  `register.generated.*`. After a bundle, `tsc` also compiles the generated file, so the dev build
  imports it and calls its `registerTools`, which re-registers every tool. The MCP SDK rejects
  duplicate tool names, so this most likely produces one caught error log line rather than a broken
  server. This is unverified at runtime; see [doc drift](../concepts/doc-drift.md).
- `server/manifest.json` (the `.mcpb` manifest) still points at `build/index.js` and describes "54
  generic Revit tools", which is stale.
