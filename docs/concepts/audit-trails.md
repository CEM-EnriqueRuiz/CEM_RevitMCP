---
title: Audit trails (Screenshots, Log)
type: concept
updated: 2026-10-06
sources: [plugin/Utils/ActionLogger.cs, plugin/Core/CommandExecutor.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs]
related: [../decisions/0013-hardcoded-failsoft-audit-trails.md, ../decisions/0008-parent-tools-from-recipe-corpus.md]
tags: [logging, audit]
---

# Audit trails (Screenshots, Log)

Decision and rationale: [0023](../decisions/0023-send-code-scripts-not-persisted.md) (current), which superseded
[0013](../decisions/0013-hardcoded-failsoft-audit-trails.md) (three trails, including the removed `Recipes\`).

Root: `C:\DC\ACCDocs\Cemengal\CMGL-TechnicalOffice\Project Files\01-Shared\02-Software\00-Revit\00-RevitAPI\05-CEMAIModeler\`

| Trail | Code | When | Format |
|---|---|---|---|
| `Screenshots\` | `TakeScreenshotEventHandler` | `take_screenshot` with `saveToDisk: true` | timestamped PNG |
| `Log\` | `ActionLogger.Log`, called in `CommandExecutor.ExecuteCommand` (reached from `SocketService` only since [0021](../decisions/0021-loopback-only-socket.md); before that the log was never written) | **every** tool call, success or failure, including `send_code_to_revit` (its `code`/`data` logged as `"<omitted>"`) | `actions_yyyyMMdd.jsonl`: `{ts, tool, input, ok, result}` |

## Using them

- **Evaluate a session:** read the day's JSONL. A healthy session shows specific `family_*`,
  `arch_*` and similar tools, not a wall of `send_code_to_revit`.
- **Decide what to build next:** count `send_code_to_revit` calls in the log and look at what the session
  was trying to do. Script bodies are **not** persisted (`"<omitted>"` in the log; the `Recipes\` dump was
  removed as noise in `fe06b6c`, so don't reintroduce it). The `parent_*` pack came from the old `Recipes\`
  corpus, where about 67% of recipes were link-geometry reconstruction
  ([0008](../decisions/0008-parent-tools-from-recipe-corpus.md)).

## Rules when touching them

- Keep them fail-soft. A logging exception must never fail a command.
- Change the log in `plugin/Utils/ActionLogger.cs`, not `PathManager` (that is the plugin-internal
  `Logs` dir).
- Keep bulky fields out of the log (`imageBase64` is replaced and results are truncated at 8000
  chars).
- Making the root configurable is an open TODO in all three files.
