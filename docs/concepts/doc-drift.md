---
title: Known doc drift and latent issues
type: concept
updated: 2026-10-06
sources: [CEM_RevitMCP.md, server/tests/TESTS.md, server/manifest.json, server/src/tools/register.ts, .github/workflows/release.yml, README.md, command.json]
related: [../decisions/0019-single-authoritative-doc.md, ../decisions/0012-drop-sqlite-store-pack.md, ../decisions/0015-single-file-bundle.md]
tags: [drift, gotchas, todo]
---

# Known doc drift and latent issues

Found at the wiki bootstrap (2026-10-06) by checking the old docs against the code. **The code is
the truth.** None of these were fixed in code; each needs its own change with an issue.

## Stale claims in docs

| Where | Claim | Reality |
|---|---|---|
| old skill, "Decisions and rationale" | `store_project_data`/`store_room_data`/`query_stored_data` are "deliberately kept" | removed in `aa88b96` ([0012](../decisions/0012-drop-sqlite-store-pack.md)); `CEM_RevitMCP.md` §5 is correct |
| old skill table, `CEM_RevitMCP.md` §4 template | `"assemblyPath": "RevitMCPCommandSet.dll"` | `CEM_IAModeler_CommandSet.dll` (all 120 entries) |
| `CEM_RevitMCP.md` §1 diagram | `RevitMCPPlugin` → `RevitMCPCommandSet` | `CEM_IAModeler` → `CEM_IAModeler_CommandSet` |
| `CEM_RevitMCP.md` §4 build checklist | `dotnet build commandset\RevitMCPCommandSet.csproj` | `commandset\CEM_IAModeler_CommandSet.csproj` |
| `CEM_RevitMCP.md` §5, header | "120 wired commands", `tag_walls`/`tag_rooms` listed | 120 in `command.json`/C#, but only **118 TS tools**: `tag_walls` and `tag_rooms` have no TS wrapper, so MCP can't reach them; `say_hello` is unlisted |
| `CEM_RevitMCP.md` §8 smoke test | MCP path `d:\repos\Cemengal\CEM_IA\CEM_RevitMCP\mcp-servers-for-revit\server\build\index.js` | `<checkout>\CEM_IA\CEM_RevitMCP\server\build\index.js` (no `mcp-servers-for-revit` level) |
| `CEM_RevitMCP.md` intro | AI-behaviour guide is the "`CEM_AIModeler` Claude skill" | it exists outside this repo (installed as `cem-aimodeler`); not verifiable from here |
| old skill | "four older plan docs were consolidated" | git history has three (`TOOLSET_ROADMAP.md`, `SPECIALIST_ARCHITECTURE.md`, `SMOKE_TEST.md`) |
| `server/tests/TESTS.md`, Traceability | no Cemengal commit touched `server/src/tools/*.ts` | `b04b41e` and `aa88b96` each touch 53 tool files |
| `server/manifest.json` | "54 generic Revit tools" | 118 TS tools |
| `README.md` (upstream text) | Node 18+, "WebSocket" transport, upstream release ZIPs | `package.json` engines `node >=20`; transport is a raw TCP socket (`net.Socket`); no Cemengal releases |
| `CEM_RevitMCP.md` §1 | `command.json` "copied to `Commands/commandRegistry.json` by the CEM_RibbonUI build" | true, and the fork's own `DeployCommandSet` target also copies it as `command.json` for standalone builds |

## Latent code issues (not fixed; report or file an issue)

- **`register.ts` does not exclude `register.generated.ts`.** After `npm run bundle`, `npm run
  build` compiles the generated file, and the dev server imports it and re-registers every tool.
  The MCP SDK throws on a duplicate tool name; the error is caught per file. Likely just one error
  line in the log, but the runtime effect is unverified. The precondition is real: at bootstrap,
  `server/build/tools/register.generated.js` existed in the local build
  ([0015](../decisions/0015-single-file-bundle.md)).
- **Release pipeline is broken for the fork:** `.github/workflows/release.yml` builds
  `mcp-servers-for-revit.sln` (renamed to `CEM_RevitMCP.sln`) with Node 18 (engines require >=20)
  and publishes the upstream npm package name ([release](../workflows/release.md)).
- **Listener binds all interfaces:** `SocketService` uses `IPAddress.Any:8080` (upstream). Any host
  that can reach port 8080 can drive Revit while the server is on.
- **`CommandManager` instantiates every command type per registry entry** (120 × ~120 reflection
  constructions at startup; upstream design). It works but is wasteful. Its success log line also
  says "Failed to create command instance", an upstream copy-paste typo.
- **Hardcoded audit root**, in three files ([0013](../decisions/0013-hardcoded-failsoft-audit-trails.md)).
- **`.gitignore` ignores `.claude/`**: the skill is tracked only because it was force-added, and
  `.claude/settings.json` needs `git add -f` or an ignore exception.
