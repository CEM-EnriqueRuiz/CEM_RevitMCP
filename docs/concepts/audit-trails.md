---
title: Audit trails (Recipes, Screenshots, Log)
type: concept
updated: 2026-10-06
sources: [plugin/Utils/ActionLogger.cs, plugin/Core/CommandExecutor.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, commandset/Services/AnnotationComponents/TakeScreenshotEventHandler.cs]
related: [../decisions/0013-hardcoded-failsoft-audit-trails.md, ../decisions/0008-parent-tools-from-recipe-corpus.md]
tags: [logging, audit]
---

# Audit trails (Recipes, Screenshots, Log)

Decision and rationale: [0013](../decisions/0013-hardcoded-failsoft-audit-trails.md).

Root: `C:\DC\ACCDocs\Cemengal\CMGL-TechnicalOffice\Project Files\01-Shared\02-Software\00-Revit\00-RevitAPI\05-CEMAIModeler\`

| Trail | Code | When | Format |
|---|---|---|---|
| `Recipes\` | `ExecuteCodeEventHandler` (`RecipesLogDirectory`) | every `send_code_to_revit`, **before** execution | the raw C# snippet, timestamped file |
| `Screenshots\` | `TakeScreenshotEventHandler` | `take_screenshot` with `saveToDisk: true` | timestamped PNG |
| `Log\` | `ActionLogger.Log`, called in `CommandExecutor.ExecuteCommand` | **every** tool call, success or failure | `actions_yyyyMMdd.jsonl`: `{ts, tool, input, ok, result}` |

## Using them

- **Evaluate a session:** read the day's JSONL. A healthy session shows specific `family_*`,
  `arch_*` and similar tools, not a wall of `send_code_to_revit`.
- **Decide what to build next:** cluster the `Recipes\` scripts. The `parent_*` pack came from
  finding that about 67% of recipes were link-geometry reconstruction
  ([0008](../decisions/0008-parent-tools-from-recipe-corpus.md)).

## Rules when touching them

- Keep them fail-soft. A logging exception must never fail a command.
- Change the log in `plugin/Utils/ActionLogger.cs`, not `PathManager` (that is the plugin-internal
  `Logs` dir).
- Keep bulky fields out of the log (`imageBase64` is replaced and results are truncated at 8000
  chars).
- Making the root configurable is an open TODO in all three files.
