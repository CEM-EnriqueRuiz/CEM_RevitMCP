---
title: "0023 send_code_to_revit scripts are not persisted; two audit trails remain"
type: decision
status: current
updated: 2026-10-07
sources: [fe06b6c, plugin/Utils/ActionLogger.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs]
related: [0013-hardcoded-failsoft-audit-trails.md, 0008-parent-tools-from-recipe-corpus.md, ../concepts/audit-trails.md, ../modules/send-code-to-revit.md]
tags: [audit, logging, send-code]
---

# 0023 send_code_to_revit scripts are not persisted; two audit trails remain

Supersedes [0013](0013-hardcoded-failsoft-audit-trails.md) (three trails, including `Recipes\`).

## Context

From `aa88b96`, `ExecuteCodeEventHandler.SaveRecipeToFile` wrote every `send_code_to_revit` script
to `…\05-CEMAIModeler\Recipes\recipe_<timestamp>.txt` before execution, and `ActionLogger` replaced
the script body in the JSONL log with a pointer to that folder.

## Decision (`fe06b6c`, 2026-10-05)

- The per-call **`Recipes\` dump is removed** (`SaveRecipeToFile` and `RecipesLogDirectory` are
  gone). The user's stated reason was that it was "noise". The skill said "don't reintroduce it".
- The action log **still omits the script body**. `ActionLogger.BuildInputToken` replaces the
  `code`/`data` params of `send_code_to_revit` with `"<omitted>"` and keeps the rest (for example
  `transactionMode`). The C# is deliberately not persisted anywhere.
- Two fail-soft trails remain under the hardcoded `…\05-CEMAIModeler\` root: `Screenshots\` (per
  `take_screenshot` with `saveToDisk`) and `Log\` (one JSONL line per tool call). The other
  properties of 0013 are unchanged: the hardcoded path with a TODO to make it configurable,
  fail-soft writes, editing `ActionLogger` rather than `PathManager`, and bulky fields replaced and
  truncated.

## Consequences

- **Every command is audited, but no script body is logged.** With the loopback/audited-dispatch
  change (decision 0021, drafted and still uncommitted on 2026-10-07), `SocketService` dispatches through
  `CommandExecutor.ExecuteCommand`, which calls `ActionLogger.Log` on every path, so
  `send_code_to_revit` calls do appear in the log, with `code` and `data` as `"<omitted>"`. Both
  meanings hold together. Without 0021, the log was never written at all (see 0013's correction).
- The "recipe corpus" in [0008](0008-parent-tools-from-recipe-corpus.md) no longer grows. Existing
  `Recipes\` files on disk are a historical corpus. New evidence for what to build next is how
  often `send_code_to_revit` appears in `Log\`, and what the session was trying to do, not the
  script text.
- Don't add script persistence back (to disk or to the log) without a new decision.
