---
title: Known doc drift and latent issues
type: concept
updated: 2026-10-07
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
| old skill table, `CEM_RevitMCP.md` §4 template | `"assemblyPath": "RevitMCPCommandSet.dll"` | `CEM_IAModeler_CommandSet.dll` (all 126 entries) |
| `CEM_RevitMCP.md` §1 diagram | `RevitMCPPlugin` → `RevitMCPCommandSet` | `CEM_IAModeler` → `CEM_IAModeler_CommandSet` |
| `CEM_RevitMCP.md` §4 build checklist | `dotnet build commandset\RevitMCPCommandSet.csproj` | `commandset\CEM_IAModeler_CommandSet.csproj` |
| `CEM_RevitMCP.md` §5 | `tag_walls`/`tag_rooms` listed as tools | **header resolved in `29b3fba`:** it now says 120 entries / 118 MCP tools, with `tag_walls`/`tag_rooms` deliberately consolidated into `tag_elements` (the C# still serves both). The §5 list still shows them, and `say_hello` is unlisted |
| `CEM_RevitMCP.md` §8 smoke test | MCP path `d:\repos\Cemengal\CEM_IA\CEM_RevitMCP\mcp-servers-for-revit\server\build\index.js` | `<checkout>\CEM_IA\CEM_RevitMCP\server\build\index.js` (no `mcp-servers-for-revit` level) |
| `CEM_RevitMCP.md` intro | AI-behaviour guide is the "`CEM_AIModeler` Claude skill" | **resolved 2026-10-07:** it was a claude.ai account skill outside this repo; it now lives here, `.claude/skills/cem-aimodeler/`, and the intro points to it ([Cemengal add-ins through the MCP](cemengal-addins-via-mcp.md)) |
| old skill | "four older plan docs were consolidated" | git history has three (`TOOLSET_ROADMAP.md`, `SPECIALIST_ARCHITECTURE.md`, `SMOKE_TEST.md`) |
| `server/tests/TESTS.md`, Traceability | no Cemengal commit touched `server/src/tools/*.ts` | `b04b41e` and `aa88b96` each touch 53 tool files |
| `server/manifest.json` | "54 generic Revit tools" | 124 TS tools |
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
- ~~**Listener binds all interfaces**~~ fixed (HOT FIX, 2026-10-07) by [0021](../decisions/0021-loopback-only-socket.md), which also fixed the never-written audit log (`SocketService` bypassed `CommandExecutor`). Original note: `SocketService` used `IPAddress.Any:8080` (upstream). Any host
  that can reach port 8080 can drive Revit while the server is on.
- **`CEM_RevitAPI_Extended` does not build for Revit 2026** (found 2026-10-07): `EXElementId.GetValue` calls
  `ElementId.IntegerValue`, which the 2026 API removed (CS1061), on `origin/master` too. CEM_RibbonUI, the MCP's
  host, cannot build R26 for that reason, and since [0025](../decisions/0025-cemengal-addins-as-tools-via-project-references.md)
  neither can the command set. A fix belongs in CEM_RevitAPI (it needs a version-dependent call, which that
  repo flags before adding).
- **`CommandManager` instantiates every command type per registry entry** (126 × ~126 reflection
  constructions at startup; upstream design). It works but is wasteful. Its success log line also
  says "Failed to create command instance", an upstream copy-paste typo.
- **Hardcoded audit root**, now in two files (`ActionLogger`, `TakeScreenshotEventHandler`). The
  third (Recipes) was removed in `fe06b6c` ([0023](../decisions/0023-send-code-scripts-not-persisted.md)).
- ~~**`.gitignore` ignores `.claude/`**~~ **resolved** in `29b3fba`: `.claude/*` is ignored except
  `agents/` and `skills/cem-revitmcp-dev/`. `.claude/settings.json` is tracked (force-added) even
  though the rule still matches it.
- **Command-set standalone deploy not guarded** (found 2026-10-07): `29b3fba` added the
  CEM_RibbonUI-sibling guard to the plugin only. `CEM_IAModeler_CommandSet.csproj` still deploys on
  any build without `CemRibbonHostBuild`, including a direct `CEM_RevitAPI.sln` Debug build
  ([0024](../decisions/0024-standalone-deploy-only-without-ribbon-host.md)).
- **Remote agent definition drifted from the wiki** (resolved at the 2026-10-07 DOCS catch-up):
  `.claude/agents/cem-revitmcp-dev.md` (`29b3fba`) called `CEM_RevitMCP.md` authoritative and said
  to update its §5/§7. It now points to `docs/index.md`.
