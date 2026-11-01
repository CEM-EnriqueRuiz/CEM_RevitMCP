---
title: Audit trails and local files (Screenshots, action log, plugin log)
type: concept
updated: 2026-11-01
sources: [https://github.com/CEM-EnriqueRuiz/CEM_API/issues/82, plugin/Utils/ActionLogger.cs, plugin/Utils/Logger.cs, plugin/Utils/McpLocalFiles.cs, plugin/Core/CommandExecutor.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, commandset/Services/Views/ExportViewImageEventHandler.cs, commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs, commandset/Utils/McpLocalFiles.cs, server/tests/guards/no-acc-writes.test.ts]
related: [../decisions/0027-send-code-scripts-traced-to-local-temp.md, ../decisions/0026-logs-and-artefacts-in-local-temp.md, ../decisions/0023-send-code-scripts-not-persisted.md, ../decisions/0013-hardcoded-failsoft-audit-trails.md, ../decisions/0008-parent-tools-from-recipe-corpus.md]
tags: [logging, audit, acc]
---

# Audit trails and local files (Screenshots, action log, plugin log)

Where: [0026](../decisions/0026-logs-and-artefacts-in-local-temp.md) (root decision 0031). What is kept:
[0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md) (the executed code, since 2026-11-01), which
superseded [0023](../decisions/0023-send-code-scripts-not-persisted.md) (no script kept), which superseded
[0013](../decisions/0013-hardcoded-failsoft-audit-trails.md) (three trails, including the removed `Recipes\`).

Root: `%TEMP%\Cemengal\RevitMCP\` (the user's own temp folder, local and per user; see below), through CEM_CSharpExtended's
`CemTempFolder` with the app name `McpLocalFiles.App`. Until 2026-10-24 the trails lived under the ACC folder
`…\00-RevitAPI\05-CEMAIModeler\`, and the plugin log lived beside the DLL, i.e. in the ACC menu. The old files there
were deleted on 2026-10-25, and the folders were kept ([CEM_API#82](../sources/issues/CEM-EnriqueRuiz-CEM_API-82.md)).
Colleagues' Revit writes there again until the menu is published to ACC.

| File | Code | When | Format |
|---|---|---|---|
| `actions-yyyyMMdd.jsonl` | `ActionLogger.Log`, called in `CommandExecutor.ExecuteCommand` (reached from `SocketService` only since [0021](../decisions/0021-loopback-only-socket.md); before that the log was never written) | **every** tool call, success or failure, including `send_code_to_revit` (its `code`/`data` logged as `"<see result.codeFile>"`); plus a `"late": true` line from `ExecuteCodeEventHandler` when a snippet ends after its call timed out | JSONL: `{ts, tool, input, ok, result}` (+ `late`) |
| `mcp-yyyyMMdd.log` | `Logger` (the RevitMCPSDK `ILogger`: socket, command loading, dispatch) | Info and above | `yyyy-MM-dd HH:mm:ss [Level] message` |
| `Screenshots\screenshot_yyyyMMdd_HHmmss_fff.png` | `TakeScreenshotEventHandler` | `take_screenshot` with `saveToDisk` (default true) | PNG; the tool returns the path as `Saved copy:` |
| `code\send_code_yyyyMMdd_HHmmss_fff_<runId>.cs` | `ExecuteCodeCommand` | every `send_code_to_revit` call, before it runs ([0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md)) | the exact C# sent; the result's `codeFile` |
| `images\view_yyyyMMdd_HHmmss_fff.png` | `ExportViewImageEventHandler` | every `export_view_image` call; never overwritten | PNG; the result's `filePath`, `bytes`, `width`, `height` |
| `SharedParameters\`, `Families\` | `create_project_parameter`, `create_shared_parameter`, `family_save_session`, `family_open_session` (a family with no file, saved so Revit can activate it) | temporary files Revit needs; see 0026 | `.txt`, `.rfa` |

The folder prunes itself on the first path asked for in each process: files older than 7 days go, then the oldest
until it holds at most 50 MB. Locked files are skipped, and pruning never throws.

**Revit's per-session temp (found and fixed on 2026-10-24):** inside Revit, `Path.GetTempPath()` is not the user's `%TEMP%`.
It is Revit's per-launch folder, `%TEMP%\<session-guid>\`, which survives Revit's exit. `CemTempFolder` therefore resolves
its root from `CemTempFolder.UserTempPath`:
1. the user's `TEMP` (HKCU, expanded);
2. otherwise `%LOCALAPPDATA%\Temp`;
3. otherwise `Path.GetTempPath()`.

The live re-check (Revit 2024, 23:12) wrote the action log, the plugin log, `Families\` and ParameterChecker's log to
`C:\Users\<user>\AppData\Local\Temp\Cemengal\…`, with nothing in the session folder. If the files ever turn up in
`%TEMP%\<guid>\Cemengal\`, Revit loaded a `CEM_CSharpExtended.dll` without `UserTempPath`: the first copy loaded wins for every
add-in, so redeploy them all. Since 2026-11-01 a family copy never overwrites another: each one is `Families\<name>_<stamp>\<name>.rfa`, in its
own folder, and empty folders are removed on the next copy. Saving the open copy again in place can still leave Revit's
own `<name>.0001.rfa` backup beside it.

## Using them

- **Evaluate a session:** read the day's JSONL on the machine that ran it. A healthy session shows specific
  `family_*`, `arch_*` and similar tools, not a wall of `send_code_to_revit`. The log is per machine and kept
  7 days.
- **See what a snippet did:** the log line's `result` names its `codeFile`, outcome (`ok`, `error`, `rolledBack`),
  document and duration; open the file for the exact code. A `"late": true` line is the real end of a call that was
  reported as timed out.
- **Decide what to build next:** count `send_code_to_revit` calls in the log, and read their `code\` files, which
  are kept 7 days per machine ([0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md)). Don't revive the
  ACC `Recipes\` dump, which was removed as noise in `fe06b6c`. The `parent_*` pack came from the old `Recipes\`
  corpus, where about 67% of recipes were link-geometry reconstruction
  ([0008](../decisions/0008-parent-tools-from-recipe-corpus.md)).

## Rules when touching them

- **No write through a junction or link (SEC-8, CWE-59, 2026-11-01).**
  - CEM_CSharpExtended's `CemTempFolder` refuses a reparse point in the folders it creates, with
    `CemTempFolderUnsafeException` (an `IOException`).
  - A path this repo derives below them is checked with `McpLocalFiles.RefuseReparsePoint`, which throws the same
    exception:
    - a family copy folder `Families\<name>_<stamp>\`: its name can be predicted, so it is checked before and after
      it is created, together with the file;
    - the fixed `SharedParameters\RevitMCP_SharedParameters.txt` and its folder: checked before and after the write.
  - `family_open_session` reports a refused copy as a clear error: nothing is saved, and the unsaved family stays
    open.
  - The empty-copy-folder sweep skips reparse points.
- Every file write goes through `CemTempFolder` (`AppendLog`, `NewArtifactFile`, `ArtifactFolder`). Never write
  under ACC (any `ACCDocs` path), next to the DLL (`PathManager` paths), to the raw `%TEMP%`, or to the Desktop.
  The guard `server/tests/guards/no-acc-writes.test.ts` fails otherwise. A new writer goes on its inventory,
  with the reason.
- Keep them fail-soft. A logging exception must never fail a command (`AppendLog` never throws).
- Change the action log in `plugin/Utils/ActionLogger.cs`. `PathManager` is read only: the deploy's `Commands\`
  and registry.
- Keep bulky fields out of the log (`imageBase64` is replaced and results are truncated at 8000
  chars).
