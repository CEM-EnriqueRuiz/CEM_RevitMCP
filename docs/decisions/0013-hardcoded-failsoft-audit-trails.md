---
title: "0013 Three fail-soft audit trails under a hardcoded ACCDocs folder"
type: decision
status: superseded
superseded_by: 0023-send-code-scripts-not-persisted.md
updated: 2026-10-06
sources: [aa88b96, plugin/Utils/ActionLogger.cs, plugin/Core/CommandExecutor.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs]
related: [../concepts/audit-trails.md, 0008-parent-tools-from-recipe-corpus.md]
tags: [logging, audit, todo]
---

# 0013 Three fail-soft audit trails under a hardcoded ACCDocs folder

**Status: superseded** by [0023](0023-send-code-scripts-not-persisted.md) (`fe06b6c`, 2026-10-05): the `Recipes\`
trail was removed and script bodies are not persisted anywhere. Everything else below still holds for the two
remaining trails (`Screenshots\`, `Log\`).

## Context

To evaluate an AI session after the fact, and to find which raw-code recipes should become tools
([0008](0008-parent-tools-from-recipe-corpus.md)), every action needs to leave a record next to the
model work.

## Decision

Three on-disk trails, all under
`C:\DC\ACCDocs\Cemengal\CMGL-TechnicalOffice\Project Files\01-Shared\02-Software\00-Revit\00-RevitAPI\05-CEMAIModeler\`:

| Folder | Written by | Content |
|---|---|---|
| `Recipes\` | `ExecuteCodeEventHandler` (commandset) | every `send_code_to_revit` script, saved **before** execution |
| `Screenshots\` | `TakeScreenshotEventHandler` | optional PNG per `take_screenshot` (`saveToDisk`) |
| `Log\` | `plugin/Utils/ActionLogger.cs`, called from `CommandExecutor.ExecuteCommand` | one JSONL line per **every** tool call: `ts, tool, input, ok, result`; file `actions_yyyyMMdd.jsonl` |

All three are **fail-soft**: a write failure goes to Debug output and never affects the command.
`ActionLogger` replaces the `code`/`data` body of `send_code_to_revit` with a pointer (the full
script is already in `Recipes\`), truncates results at 8000 chars, and `DescribeResult` replaces
`imageBase64` with a placeholder.

## Consequences

- The path is hardcoded in three places, each marked `TODO: make configurable`. On a machine
  without that ACCDocs folder, the trails silently write nothing, or create the folder if `C:\DC`
  is writable.
- To change the log, edit `plugin/Utils/ActionLogger.cs`, **not** `PathManager`, which points at a
  different plugin-internal `Logs` dir.
- `CommandExecutor.ExecuteCommand` is the single dispatch chokepoint. Anything that must happen on
  every call goes there.
