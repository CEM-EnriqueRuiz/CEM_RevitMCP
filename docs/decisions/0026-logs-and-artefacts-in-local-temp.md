---
title: "0026 Logs and temporary artefacts go to %TEMP%\\Cemengal\\RevitMCP; nothing writes under ACC or beside the DLL"
type: decision
status: current
updated: 2026-10-25
sources: [https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82, "root decision 0031 (the user, 2026-10-24 and 2026-10-25)", plugin/Utils/ActionLogger.cs, plugin/Utils/Logger.cs, plugin/Utils/PathManager.cs, plugin/Utils/McpLocalFiles.cs, plugin/Configuration/ConfigurationManager.cs, plugin/UI/CommandSetSettingsPage.xaml.cs, commandset/Utils/McpLocalFiles.cs, commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs, commandset/Services/Core/CoreParamQueryEventHandlers.cs, commandset/Services/Core/CreateSharedParameterEventHandler.cs, commandset/Services/Family/FamilySessionEventHandlers.cs, server/tests/guards/no-acc-writes.test.ts]
related: [../sources/issues/CEM-EnriqueRuiz-CEM_API-82.md, 0023-send-code-scripts-not-persisted.md, 0013-hardcoded-failsoft-audit-trails.md, 0011-cem-ribbonui-hosts-deployment.md, 0024-standalone-deploy-only-without-ribbon-host.md, ../concepts/audit-trails.md, ../modules/plugin.md, ../modules/commandset.md, ../workflows/test.md]
tags: [logging, audit, acc, deploy, cem_csharpextended]
---

# 0026 Logs and temporary artefacts go to %TEMP%\Cemengal\RevitMCP; nothing writes under ACC or beside the DLL

This repo's application of root decision
[0031](../../../../docs/decisions/0031-nothing-writes-under-acc-logs-in-local-temp.md). It supersedes the location
clause of [0023](0023-send-code-scripts-not-persisted.md): the hard-coded `…\05-CEMAIModeler\` root.

## Context

On colleagues' machines, CEM_RibbonUI and this plugin load from the ACC menu
(`…\00-RevitAPI\00-Menu\CEM_RibbonUI`), which Desktop Connector syncs to everyone. The user asked on 2026-10-24
that nothing of ours writes under ACC, and that every log goes to a local temp folder that cleans itself. The
audit that day found these writes:

| Write | Where | Kind |
|---|---|---|
| `ActionLogger` | ACC `05-CEMAIModeler\Log\actions_*.jsonl`, hard-coded | log |
| `Logger` (through `PathManager.GetLogsDirectoryPath`) | `<DLL>\Logs\mcp_*.log`, i.e. ACC `00-Menu\CEM_RibbonUI\Logs` (11 files) | log |
| `TakeScreenshotEventHandler` (`saveToDisk` defaults to true) | ACC `05-CEMAIModeler\Screenshots`, hard-coded | temp artefact |
| `PathManager.GetCommandsDirectoryPath` / `GetCommandRegistryFilePath` | created `<DLL>\Commands\` and an empty `commandRegistry.json` when it was missing | shipped data |
| `CommandSetSettingsPage` | rewrote `commandRegistry.json` beside the DLL on Save **and on load** | shipped data |
| `ProjectUtils.SaveToDesktop` | a StreamWriter to the Desktop; upstream debug helper with no caller | dead code |
| `create_project_parameter` | `%TEMP%\mcp_proj_<guid>.txt` on every call, never deleted | temp artefact |
| `create_shared_parameter` | `%TEMP%\RevitMCP_SharedParameters.txt` when the user has none (Revit remembers it in Revit.ini); **new definitions into the user's configured file**, which may be the team's file on ACC | temp artefact / shared team data |
| `family_save_session` | `%TEMP%\<title>.rfa` when the family has no path; **`famDoc.Save()` in place** when it has one, ACC included | temp artefact / user data |

`ExecuteCodeEventHandler.SaveRecipeToFile` wrote the `Recipes\` folder. It was removed in `fe06b6c` ([0023](0023-send-code-scripts-not-persisted.md)).
The 2026-10-06 recipes came from a Revit still running the older DLL. The Node server writes no file.

On 2026-10-25 the user kept every change. Root 0031 has no exception for the MCPs; the add-ins' own team files on ACC
(presets, the Numerate trace) are a separate matter and stay. Issue:
[CEM_API#82](../sources/issues/CEM-EnriqueRuiz-CEM_API-82.md).

## Decision

All of it goes through CEM_CSharpExtended's `Hosting.CemTempFolder` (netstandard2.0, BCL only), under the app
name `RevitMCP` (`McpLocalFiles.App`, one constant in each project):

- **Logs:** `ActionLogger` writes `%TEMP%\Cemengal\RevitMCP\actions-yyyyMMdd.jsonl` with
  `CemTempFolder.AppendLog(App, "actions", line, ".jsonl")`. The lines are unchanged:
  `{ts, tool, input, ok, result}`, with `send_code_to_revit`'s `code`/`data` given as `"<omitted>"`. `Logger`
  writes `mcp-yyyyMMdd.log` with `AppendLog(App, "mcp", line)`, in the same line format. It resolves the day on
  each line and creates nothing before the first one. Both are thread-safe and never throw.
- **Temporary artefacts, one kind folder each:**
  - `Screenshots\screenshot_yyyyMMdd_HHmmss_fff.png` (`NewArtifactFile`). `take_screenshot` still returns the
    image inline and `Saved copy: <path>`.
  - `SharedParameters\project_parameter_<stamp>.txt`, used for one `create_project_parameter` call. The user's
    shared-parameter file is now restored in `finally`, where an exception used to leave Revit pointing at the
    temp file. The file is deleted best-effort.
  - `SharedParameters\RevitMCP_SharedParameters.txt` (`ArtifactFolder`, fixed name). This is the fallback when
    the user has no shared-parameter file: Revit keeps pointing at it, so later calls reuse it for the session.
  - `SharedParameters\acc_copy_<stamp>.txt`, when the user's configured file is on ACC. The call copies the team
    file, works on the copy, so existing definitions keep their GUIDs, and points Revit back at the ACC file in
    `finally`. The tool warns that new definitions are in the copy and the project, not in the team file.
  - `Families\<family name>.rfa` (`ArtifactFolder`, fixed name). A family's name in the project is its file
    name, so a stamped name would rename the family.
- **The folder prunes itself:** on the first path asked for in each process, files older than 7 days go, then
  the oldest until the folder is at most 50 MB. Locked files are skipped. Pruning runs once per process, so a
  file Revit uses in this session is not pruned under it.
- **No in-place MCP save on ACC.** `family_save_session` saves in place only on a path without an `ACCDocs`
  segment (`McpLocalFiles.IsUnderAcc`). A library `.rfa` on ACC is shared team data. The tool saves a copy to
  `Families\`, the open document moves onto the copy and loads from it, and the tool warns that the ACC
  original is untouched and names the copy. `create_shared_parameter` applies the same rule to a team shared
  parameter file on ACC (above). No other in-place save is reachable from an MCP tool: `Save` and `SaveAs` appear
  only in `family_save_session`, the family is closed with `Close(false)`, and only `create_shared_parameter`
  writes definitions into a file the user configured.
- **Beside the DLL is read only.** `PathManager` only computes paths: no `CreateDirectory`, no default
  registry, and `GetLogsDirectoryPath` is gone. A missing registry loads an empty `FrameworkConfig`, and the
  error log names the path. The command-set settings page is **read-only**: it never rewrites the registry on
  load, and Save explains that the registry ships with the deploy (change `command.json` and redeploy).
- `SaveToDesktop` is deleted.
- **Guard:** `server/tests/guards/no-acc-writes.test.ts` (vitest, in the fast loop) reads the C# source. It
  fails when:
  - a source or project file holds an ACC path;
  - code uses the raw temp folder, the Desktop or Documents;
  - the Node server writes a file;
  - a file that derives a path from the add-in folder (`Assembly.Location`, `PathManager.`) writes;
  - a C# file not on its inventory writes, or an inventoried one takes its path from somewhere other than
    `CemTempFolder`.

  It also pins the behaviours above and the `CEM_CSharpExtended.csproj` ProjectReferences.

## Rejected alternatives

- **Settings page:** deleting the upstream settings UI (`Core.Settings`, `UI/*`) was rejected as a larger
  divergence from upstream for no gain. A per-user overlay in `%APPDATA%` was also rejected: it needs a Revit
  home in `CemUserHome` and a merge in `CommandManager`, and no one edits per-user tool toggles. The page is
  unreachable in the Cemengal build: no ribbon button, no `.addin` entry, and `OnStartup` is empty. Only
  RevitAddinManager or `send_code_to_revit` could reach it. In the Cemengal layout its upstream load would
  have rewritten the shipped registry **empty**, because it finds no `Commands\<set>\command.json`.
- **Families:** a per-save subfolder named by `NewArtifactFile`'s stamp would leave empty folders behind,
  because the helper prunes files, not folders. A stamped file name would rename the family. Hence
  `ArtifactFolder` with the title.
- **A make-configurable setting for the root** (the TODO of 0013/0023): not needed. The root is per-user and
  local by construction.

## Consequences

- **Found live and fixed on 2026-10-24:**
  - The bug: Revit sets its process temp to a per-launch `%TEMP%\<session-guid>\`, so the first live run wrote
    `%TEMP%\<session-guid>\Cemengal\RevitMCP\`, a folder that survives exit and that pruning never revisits.
  - The fix, in CEM_CSharpExtended: `CemTempFolder.UserTempPath` takes the user's `TEMP` (HKCU, expanded), otherwise
    `%LOCALAPPDATA%\Temp`, otherwise `Path.GetTempPath()`. This repo's code did not change for it.
  - The re-check, after a CEM_RibbonUI redeploy: the action log, the plugin log, `Families\` and ParameterChecker's log
    landed in `C:\Users\<user>\AppData\Local\Temp\Cemengal\…`, and nothing in the session folder. Nothing was written under ACC
    or beside the DLLs in either run.
- Colleagues get the fix only when the menu is published to ACC. The user chose to publish the Revit menu, with this
  plugin and the `05-CEMAIModeler\Server` copy, after the commit; the coordinator does it with the user's OK. Until then
  their Revit keeps writing `00-Menu\CEM_RibbonUI\Logs` and `05-CEMAIModeler\Log` and `Screenshots`.
- On 2026-10-25, at the user's request, the coordinator deleted the old files on ACC: `CEM_RibbonUI\Logs` (11),
  `05-CEMAIModeler\Log` (5), `Screenshots` (64) and `Recipes` (68). The folders were kept. The older
  `05-CEMAIModeler\Logs` (June) was not on the list.
- The action log is per machine. Evaluating a colleague's session means reading their `%TEMP%`.
- `CEM_CSharpExtended.dll` ships beside `CEM_IAModeler.dll` and in `Commands\`. It already did, through
  `CEM_RevitAuth`, and the CEM_RibbonUI stage copies the whole command-set output.
- `send_code_to_revit` runs arbitrary AI code, which can still write anywhere. The `cem-aimodeler` skill tells
  the AI to keep files in `%TEMP%\Cemengal\RevitMCP`.
