---
title: "CEM-EnriqueRuiz/CEM_API#82: the MCP writes nothing under ACC or beside the DLL; logs in %TEMP%\\Cemengal\\RevitMCP"
type: source
updated: 2026-11-08
sources: [https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82, "root decision 0031", plugin/Utils/ActionLogger.cs, plugin/Utils/Logger.cs, plugin/Utils/PathManager.cs, plugin/UI/CommandSetSettingsPage.xaml.cs, commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs, commandset/Services/Core/CoreParamQueryEventHandlers.cs, commandset/Services/Core/CreateSharedParameterEventHandler.cs, commandset/Services/Family/FamilySessionEventHandlers.cs, server/tests/guards/no-acc-writes.test.ts]
related: [../../decisions/0026-logs-and-artefacts-in-local-temp.md, ../../decisions/0023-send-code-scripts-not-persisted.md, ../../concepts/audit-trails.md, ../../concepts/doc-drift.md, ../../workflows/smoke-test.md, ../../../../../docs/decisions/0031-nothing-writes-under-acc-logs-in-local-temp.md, ../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-82.md]
tags: [issue, acc, logging, audit, cem_csharpextended]
---
# CEM_API#82: the MCP writes nothing under ACC or beside the DLL

**Issue:** https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82 ("Borrar logs in ACC de los MCP"). The body is empty.
The spec is the user's request of 2026-10-24, root decision
[0031](../../../../../docs/decisions/0031-nothing-writes-under-acc-logs-in-local-temp.md) and the coordinator's brief. The
cross-repo picture is on the root page [CEM_API#82](../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-82.md). This repo's
decision is [0026](../../decisions/0026-logs-and-artefacts-in-local-temp.md).

- **Asked:** check that the MCPs write no log or other file under an ACC path, because Desktop Connector syncs every write to
  every colleague. Any log goes to a local temp folder that cleans itself.
- **Implemented:** 2026-10-24 (pass 1), re-checked live the same night after a CEM_CSharpExtended fix. **Committed:** 2026-10-25.

## What changed

| Area | Change |
|---|---|
| `plugin/Utils/ActionLogger.cs` | from the hard-coded ACC `05-CEMAIModeler\Log` to `CemTempFolder.AppendLog(App, "actions", line, ".jsonl")`; same `{ts, tool, input, ok, result}` line |
| `plugin/Utils/Logger.cs` | from `<DLL>\Logs` to `AppendLog(App, "mcp", line)` |
| `plugin/Utils/PathManager.cs` | computes paths only: no `CreateDirectory`, no default registry, `GetLogsDirectoryPath` deleted |
| `plugin/Configuration/ConfigurationManager.cs` | a missing registry loads an empty `FrameworkConfig`; the log names the path |
| `plugin/UI/CommandSetSettingsPage.xaml.cs` | read-only |
| `commandset/…/TakeScreenshotEventHandler.cs` | `Screenshots\` artefact; the tool's return is unchanged |
| `commandset/…/CoreParamQueryEventHandlers.cs` | `create_project_parameter`: temp file in `SharedParameters\`, deleted after use; the user's file restored in `finally` |
| `commandset/…/CreateSharedParameterEventHandler.cs` | fallback file in `SharedParameters\`; a team file on ACC is worked on through a temp copy |
| `commandset/…/FamilySessionEventHandlers.cs` | `family_save_session` saves in place only off ACC; otherwise a copy in `Families\` |
| `commandset/Utils/ProjectUtils.cs` | dead `SaveToDesktop` deleted |
| `plugin/Utils/McpLocalFiles.cs`, `commandset/Utils/McpLocalFiles.cs` (new) | `App = "RevitMCP"`; `IsUnderAcc` in the command set |
| both csproj | ProjectReference to `CEM_CSharpExtended.csproj` (the command set's with `GlobalPropertiesToRemove="CemRibbonHostBuild"`) |
| `server/src/tools/take_screenshot.ts`, `family_save_session.ts`, `create_shared_parameter.ts` | descriptions say where files go; no schema change |
| `server/tests/guards/no-acc-writes.test.ts` (new), `server/tests/TESTS.md` | the guard, 16 tests |
| skills | `cem-revitmcp-dev` hard rule 13; `cem-aimodeler`: the action log path, and snippets write only under `%TEMP%\Cemengal\RevitMCP` |
| wiki | new [0026](../../decisions/0026-logs-and-artefacts-in-local-temp.md); [0023](../../decisions/0023-send-code-scripts-not-persisted.md)'s location clause superseded; [audit trails](../../concepts/audit-trails.md) rewritten; [doc drift](../../concepts/doc-drift.md), [plugin](../../modules/plugin.md), [commandset](../../modules/commandset.md), [tests](../../modules/tests.md), [test](../../workflows/test.md), [smoke test](../../workflows/smoke-test.md), [index](../../index.md) |

## The audit: every file write found

| Write | Kind | Now |
|---|---|---|
| `ActionLogger`, ACC `05-CEMAIModeler\Log\actions_*.jsonl` (hard-coded) | log | `%TEMP%\Cemengal\RevitMCP\actions-yyyyMMdd.jsonl` |
| `Logger`, `<DLL>\Logs\mcp_*.log`: the ACC menu `00-Menu\CEM_RibbonUI\Logs` on colleagues' machines (11 files) | log | `…\RevitMCP\mcp-yyyyMMdd.log` |
| `take_screenshot`, ACC `05-CEMAIModeler\Screenshots` (hard-coded, `saveToDisk` on by default) | temp artefact | `…\RevitMCP\Screenshots\` |
| `PathManager` created `<DLL>\Commands\` and an empty `commandRegistry.json` | shipped data | removed |
| `CommandSetSettingsPage` rewrote the registry on Save **and on load** | shipped data | read-only |
| `ProjectUtils.SaveToDesktop` (no callers) | dead code | deleted |
| `create_project_parameter`, `%TEMP%\mcp_proj_<guid>.txt`, never deleted | temp artefact | `SharedParameters\project_parameter_<stamp>.txt`, deleted |
| `create_shared_parameter`, `%TEMP%\RevitMCP_SharedParameters.txt` fallback | temp artefact | `SharedParameters\RevitMCP_SharedParameters.txt` |
| `create_shared_parameter` adds definitions to the user's configured file, which may be the team file on ACC | team data | ACC: a temp copy, setting restored, warning |
| `family_save_session`, `%TEMP%\<title>.rfa`, or `famDoc.Save()` in place, ACC included | temp artefact / team data | `Families\<name>.rfa`; in place only off ACC |
| `Recipes\recipe_*.txt` (68 on ACC, the last on 2026-10-06) | script dump | written by `SaveRecipeToFile` (`aa88b96`), removed in `fe06b6c`; the 10-06 files came from a Revit still on the old DLL |
| `server/` (Node) | none | it writes no file at runtime; the guard pins it |

## Decisions and reasons

- **All writes go through CEM_CSharpExtended's `Hosting.CemTempFolder` (in `IO` since [CEM_CSharpExtended#1](CEMENGAL-CEM_CSharpExtended-1.md)),** with one `App` constant per project. Root 0031 makes
  it the one C# implementation. Its pruning (7 days, 50 MB, once per process) and its thread-safe, never-throwing `AppendLog`
  replace this repo's own lock and path code.
- **The settings page becomes read-only.** In the Cemengal layout its upstream load finds no `Commands\<set>\command.json`, and
  it rewrote the shipped registry **empty** beside the DLL. Nothing reaches the page in the Cemengal build: no button, no
  `.addin` entry, an empty `OnStartup`. Rejected: deleting the upstream settings UI, a larger divergence from upstream for no
  gain; a per-user overlay in `%APPDATA%`, which needs a Revit home in `CemUserHome` and a merge in `CommandManager` for tool
  toggles no one edits. The coordinator approved read-only.
- **A missing registry loads no commands** instead of creating one beside the DLL. That folder may be the ACC menu.
- **`Families\<name>.rfa` uses `ArtifactFolder` with a fixed name.** A family's name in the project is its file name, so a
  stamped name would rename the family. Rejected: a per-save subfolder named by `NewArtifactFile`'s stamp, which leaves empty
  folders behind (the helper prunes files, not folders). The name comes from the original file name, or the Title without
  `.rfa`: the old code would have saved `00-PLATE-S1.rfa.rfa`.
- **No MCP save in place on ACC.** A library `.rfa` or a team shared-parameter file on ACC is team data. The MCP rule has no
  exception (root 0031), so `family_save_session` saves a copy and `create_shared_parameter` works on a copy. Both warn and
  name the copy.
- **The `create_project_parameter` restore moves into `finally`.** An exception used to leave Revit pointing at the temp
  shared-parameter file. This is a bug fix found by the audit.
- **`SharedParameters\RevitMCP_SharedParameters.txt` keeps a fixed name.** Revit keeps pointing at the fallback, so later
  calls in the session reuse it.
- **Tests:** the C# handler logic is Revit-bound (TDD exemption, accepted by the coordinator). A vitest guard reads the C#
  source instead. It fails on:
  - an ACC path;
  - raw temp, Desktop or Documents;
  - a Node file write;
  - a write in a file that derives a path from the add-in folder;
  - a writer missing from its inventory, or one that takes its path from somewhere other than `CemTempFolder`.

  It also pins each behaviour above. Rejected: a make-configurable root, the old TODO of 0013 and 0023. The root is per
  user and local by construction.

## The user's decisions on 2026-10-25

- Keep every pass-1 change.
- Root 0031 is strict for the MCPs. The add-ins keep the team files they write on ACC by design (the `02-JSON` presets, the
  Numerate trace, NestedManager and PassValue saving over a family's library `.rfa`). This repo is an MCP, so all of 0026
  stands.
- The coordinator deleted the 148 old files from ACC: `CEM_RibbonUI\Logs` (11), `05-CEMAIModeler\Log` (5), `Screenshots` (64)
  and `Recipes` (68). The folders were kept. The older `05-CEMAIModeler\Logs` (June) was not on the list.
- The ACC publish of the Revit menu, with this plugin and the `05-CEMAIModeler\Server` copy, comes later. The coordinator does
  it with the user's OK. Until then, colleagues' Revit keeps writing the old ACC paths.
- The doc drift found in pass 1 stays reported in [doc drift](../../concepts/doc-drift.md), not fixed.
- Claude Desktop's `revit-mcp` on the development machine runs
  `C:\Program Files\nodejs\node.exe D:\repos\Cemengal\CEM_IA\CEM_RevitMCP\server\build\index.js` since 2026-10-25. A
  tools/list handshake returned 124 tools, 6 of them `cem_*`. Before, it ran the June 15 bundle in
  `C:\00-CMOT\…\05-CEMAIModeler\Server\index.js`, byte-identical to ACC `Server\index.txt`: 118 tools, no `cem_*`, connecting
  to `localhost` from before 0021. That bundle imports only `net` and `node:process`, so it never wrote a file. Colleagues'
  ACC copy stays the June one until the publish.

## Verification

- **Tests:** red first, 14 failed and 47 passed (61): the guard's checks against the old code. Green afterwards: 62/62 in 8
  files, including the 16th guard test (the shared-parameter ACC copy), added with its fix. Mutation checks:
  - a planted C# writer with an ACC path and a `PathManager` path fails 3 tests;
  - removing `IsUnderAcc` from `family_save_session` fails 1;
  - removing it from `create_shared_parameter` fails 1.

  `npm run build` passes. The plugin, the command set and CEM_RibbonUI build in Debug R24.
- **Live, pass 1** (Revit 2024.3, a `%TEMP%` copy of the test model):
  - The RDP session did not render, so a throwaway starter add-in posted the ribbon command (see the
    [smoke test](../../workflows/smoke-test.md) triage).
  - Every call worked: statistics, find, `cem_swap_find_types`, a level created then deleted, `create_project_parameter`
    (its temp file deleted, the setting restored), and `family_save_session`.
  - A family opened from a fake `ACCDocs` path left the original byte-identical and saved the copy to `Families\`.
  - `take_screenshot` failed on GDI capture ("Controlador no válido") and wrote no file. It is not proven live; the guard
    pins it.
  - Inside Revit, the user's shared-parameter file is on ACC. That is why the `create_shared_parameter` copy rule matters. The
    tool was not run live, to avoid reading ACC.
- **Found live:** Revit 2024 sets its process temp to a per-launch `%TEMP%\<session-guid>\`, so the files landed there.
  CEM_CSharpExtended added `CemTempFolder.UserTempPath`. This repo's code did not change for it.
- **Live, re-check** after a CEM_RibbonUI redeploy (2026-10-24, 23:07–23:13):
  - Exactly four files landed in `C:\Users\<user>\AppData\Local\Temp\Cemengal\`: `RevitMCP\mcp-20261024.log`,
    `RevitMCP\actions-20261024.jsonl` (7 lines), `RevitMCP\Families\00-PLATE-S1.rfa`, and ParameterChecker's probe log under
    `RevitAPI\`.
  - Nothing landed in the session folder, during the run or after exit.
- **Watcher** on `C:\DC\ACCDocs` and the deployed add-in folders: 0 ACC events in both live runs.
  - No event during either Revit session.
  - The only events were the deploys' writes and the starter `.addin` being created and deleted.
  - Nothing was written beside the deployed DLLs: no `Logs\`, and `commandRegistry.json` untouched.
- **Disclosed:** during the audit, a hash of ACC `Server\index.txt` and a Git Bash listing of `Screenshots` made Desktop
  Connector download those files. Since then, ACC was only listed (names, sizes, times).
