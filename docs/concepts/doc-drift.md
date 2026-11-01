---
title: Known doc drift and latent issues
type: concept
updated: 2026-11-01
sources: [CEM_RevitMCP.md, server/tests/TESTS.md, server/manifest.json, server/src/tools/register.ts, .github/workflows/release.yml, README.md, command.json]
related: [../decisions/0026-logs-and-artefacts-in-local-temp.md, ../decisions/0019-single-authoritative-doc.md, ../decisions/0012-drop-sqlite-store-pack.md, ../decisions/0015-single-file-bundle.md]
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
| `server/manifest.json` | "54 generic Revit tools" | 125 TS tools (2026-11-01) |
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
- ~~**Revit's per-session temp folder**~~ **resolved** 2026-10-24 (found live the same day): Revit 2024 sets its process
  TMP/TEMP to `%TEMP%\<session-guid>\`, so `Path.GetTempPath()` inside Revit lands there. CEM_CSharpExtended's
  `CemTempFolder` now resolves `UserTempPath` (the user's TEMP) instead, and a live re-check confirmed it
  ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)). Any other code in Revit that calls `Path.GetTempPath()`
  directly still gets the session folder.
- **`CommandManager` instantiates every command type per registry entry** (126 × ~126 reflection
  constructions at startup; upstream design). It works but is wasteful. Its success log line also
  says "Failed to create command instance", an upstream copy-paste typo.
- ~~**Hardcoded audit root**~~ **resolved** 2026-10-24 by [0026](../decisions/0026-logs-and-artefacts-in-local-temp.md):
  `ActionLogger` and `TakeScreenshotEventHandler` wrote under ACC; both now go to `%TEMP%\Cemengal\RevitMCP`. The
  third (Recipes) was removed in `fe06b6c` ([0023](../decisions/0023-send-code-scripts-not-persisted.md)).
- ~~**The settings page empties the shipped registry**~~ **resolved** 2026-10-24 by
  [0026](../decisions/0026-logs-and-artefacts-in-local-temp.md) (found then): the upstream `CommandSetSettingsPage`
  "cleaned" `commandRegistry.json` on load. In the Cemengal layout it finds no `Commands\<set>\command.json`, so it
  rewrote the registry empty, beside the DLL. The page is read-only now.
- ~~**`create_project_parameter` leaves Revit on a temp shared parameter file**~~ **resolved** 2026-10-24
  ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)): on an exception it skipped restoring
  `SharedParametersFilename`; the restore is in `finally` now.
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

## Found 2026-10-24 (the [0026](../decisions/0026-logs-and-artefacts-in-local-temp.md) pass; reported, not fixed)

| Where | Claim | Reality |
|---|---|---|
| `CEM_RevitMCP.md` (intro, lines 37 and 277) | two logs under `…\05-CEMAIModeler\`, `Log\actions_*.jsonl` | `%TEMP%\Cemengal\RevitMCP\actions-yyyyMMdd.jsonl` and `Screenshots\` ([audit trails](audit-trails.md)) |
| [0008](../decisions/0008-parent-tools-from-recipe-corpus.md), [0021](../decisions/0021-loopback-only-socket.md), `sources/commits/2026-10.md` | `Log\actions_*.jsonl` | the same log, now `%TEMP%\Cemengal\RevitMCP\actions-<date>.jsonl`; these record history and were left as written |
| [0024](../decisions/0024-standalone-deploy-only-without-ribbon-host.md), Context | "`PathManager` creates one [registry] when none exists" | true then; since 0026 `PathManager` creates nothing and a missing registry loads no commands |
| [build and deploy](../workflows/build-and-deploy.md) | use the Cemengal-provisioned SDK `%LOCALAPPDATA%\Cemengal\dotnet\dotnet.exe` | absent on the development machine on 2026-10-24; `dotnet` on PATH (SDK 10.0.402) built R24 |
| [build and deploy](../workflows/build-and-deploy.md), [smoke test](../workflows/smoke-test.md) | the ribbon button is **CEM_RevitAI** | CEM_RibbonUI adds it as `CEM_AIModeler` (`AddPushButton<MCPServiceConnection>("CEM_AIModeler")`), as the `cem-aimodeler` skill says |

## Found in the 2026-11-01 inventory (for the modelling agent)

| Where | Claim | Reality and status |
|---|---|---|
| `cem-revitmcp-dev` skill | "118 tools … 120 commands" | **resolved 2026-11-01:** 125 TS tools, 127 commands |
| [family editing](../modules/family-editing.md), session step 2, and `family_open_session`'s message | the editor tools act "on the open or active family document" | they act on the **active** one only. **Resolved 2026-11-01** in code: `family_open_session` now activates the family; the page and the message say so |
| [0018](../decisions/0018-send-code-transaction-mode.md), Consequences | `ActionLogger` "replaces only the code body with a pointer" | it wrote `"<omitted>"` (0023); since [0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md) it writes `"<see result.codeFile>"`. **Resolved** |
| [send code](../modules/send-code-to-revit.md) | nothing on the 60 s / 120 s timeouts, the snippet running on after one, the active document as the only target, rollback in `auto` only, no failure preprocessor, the memory growth, the Chinese errors | **resolved 2026-11-01:** all documented there and in 0018 |
| [toolset](../modules/toolset.md) | `operate_element` under `Access`, the read folder, with no note | it deletes, hides, colours and makes section boxes. **Resolved:** noted, and the access tags section says folders are no guide |

Latent, not fixed:

- **`WaitForCompletion` calls `_resetEvent.Reset()` before it waits** in every upstream-pattern handler (for example
  `FamilySessionEventHandlers.cs`, `ExportViewImageEventHandler.cs`, which follows the pattern). A completion that
  lands before the wait begins is lost as a false timeout. The window is small: the event runs on the UI thread after
  `Raise`. Fixed in `send_code_to_revit` ([0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md)), where the run
  owns its event, and in `export_view_image` (2026-11-01), whose request setter re-arms the signal before the raise. The
  other handlers keep it.
- ~~**`OpenAndActivateDocument` from an external event**~~ **verified 2026-11-01:** it works from an `ExternalEvent`
  handler in Revit 2024. The API forbids it only inside Revit's own event handlers and with a transaction open.
