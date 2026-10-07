---
title: server/ — the TypeScript MCP server
type: module
updated: 2026-10-07
sources: [server/package.json, server/src/index.ts, server/src/index.bundle.ts, server/src/tools/register.ts, server/src/utils/ConnectionManager.ts, server/src/utils/SocketClient.ts, server/scripts/bundle.mjs, server/manifest.json]
related: [../concepts/four-part-tool-pattern.md, ../decisions/0015-single-file-bundle.md, ../decisions/0016-schema-only-ts-unit-tests.md, ../workflows/build-and-deploy.md]
tags: [server, typescript]
---

# server/ — the TypeScript MCP server

**Origin: upstream**, extended by Cemengal. Since the last upstream commit (`86cf705`), Cemengal
added 97 tool files, deleted 8 and renamed one (`color_elements` → `color_splash`), and added the
bundle scripts and the vitest suite. This is what the LLM sees: a stdio MCP server whose tools forward to Revit over TCP.

## Layout

| Path | Role |
|---|---|
| `src/index.ts` | dev entry: `McpServer({name: "mcp-server-for-revit"})`, `await registerTools`, stdio transport |
| `src/index.bundle.ts` | bundle entry: identical, but uses `tools/register.generated.ts` |
| `src/tools/<name>.ts` | one file per tool (124), each exporting `register<Name>Tool(server)` |
| `src/tools/register.ts` | runtime discovery: `readdirSync` + dynamic import, calls the first `register*` export |
| `src/tools/register.generated.ts` | **generated, gitignored**: static imports for esbuild |
| `src/utils/ConnectionManager.ts` | `withRevitConnection(op)`: global mutex, new socket per call, 5 s connect timeout |
| `src/utils/SocketClient.ts` | `RevitClientConnection`: `net.Socket` to `127.0.0.1:8080` (`REVIT_HOST`/`REVIT_PORT` in `ConnectionManager.ts`, [0021](../decisions/0021-loopback-only-socket.md)), JSON-RPC, buffers until JSON parses, 120 s timeout |
| `scripts/generate-tool-registry.mjs` | writes `register.generated.ts` |
| `scripts/bundle.mjs` | esbuild → `dist/index.js` (single ESM file, node20) |
| `manifest.json` | `.mcpb` manifest (`cem-revit-ai`), entry `build/index.js` |
| `tests/` | vitest schema suite ([0016](../decisions/0016-schema-only-ts-unit-tests.md)) |

## Scripts (`package.json`)

- `npm run build`: `rimraf build` then `tsc` → `build/index.js` (what the MCP client runs in dev).
- `npm run bundle`: generate the registry, then esbuild → `dist/index.js`.
- `npm test`: `vitest run`.

The package identity is still upstream's (`mcp-server-for-revit`, upstream repo URLs). Node `>=20`.
Dependencies: `@modelcontextprotocol/sdk`, `ws`, `zod` (no native modules since
[0012](../decisions/0012-drop-sqlite-store-pack.md)).

## Rules

- Tool name == `command.json` name ([0001](../decisions/0001-extend-via-four-part-pattern.md)).
- Describe every zod field; the LLM reads them.
- Catch errors and return a text content block. Never throw out of the handler.
- Images: `{ type: "image", data, mimeType }` (see `take_screenshot.ts`). Keep payloads bounded.
- Many upstream comments and log strings are in Chinese. Leave them; write new code in English.

## Gotchas

- Calls are serialized by the mutex. A slow tool blocks every other tool call.
- The `register.generated` double-registration issue: [0015](../decisions/0015-single-file-bundle.md).
