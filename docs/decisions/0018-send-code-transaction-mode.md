---
title: "0018 send_code_to_revit takes a transactionMode (auto | none)"
type: decision
status: current
updated: 2026-11-01
sources: [17d0108, 86cf705, server/src/tools/send_code_to_revit.ts, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs]
related: [../modules/send-code-to-revit.md, 0027-send-code-scripts-traced-to-local-temp.md]
tags: [send-code, transactions, upstream]
---

# 0018 send_code_to_revit takes a transactionMode (auto | none)

**Origin: upstream** (`17d0108`, merged as upstream PR #10 in `86cf705`).

## Context

`send_code_to_revit` used to always wrap the snippet in a transaction. Code that manages its own
transactions (for example, opening and saving a family document) could not run.

## Decision

The tool takes `transactionMode`: `"auto"` (the default) wraps the snippet in a transaction, and
`"none"` runs it bare so the code manages its own. Any value other than `"none"` is treated as
`"auto"` (`ExecuteCodeEventHandler.SetExecutionParameters`).

## What each mode guarantees (documented 2026-11-01, from the code)

| | `auto` | `none` |
|---|---|---|
| Transaction | one `Transaction(doc, "执行AI代码")` on the **active** document, opened after the snippet compiles | none: the snippet opens, commits and rolls back its own |
| The snippet throws | the uncommitted transaction is disposed, which **rolls back** its changes to the active document; `outcome: "rolledBack"` | **nothing is rolled back**; whatever it committed stays; `outcome: "error"` |
| Revit's commit fails (`Commit()` is not `Committed`) | `outcome: "rolledBack"` | n/a |
| Compile error | nothing ran; `outcome: "error"` | the same |
| Other open documents, files, `Save`/`Close` | never covered | never covered |

- **The 60 s timeout does not stop the snippet.** It keeps running on Revit's UI thread. Since
  [0027](0027-send-code-scripts-traced-to-local-temp.md), the call says whether it had started, and a late completion
  appends its real outcome to the action log. A run Revit had not started by then never runs. The TS client waits
  120 s, so the plugin's 60 s answer comes first.
- **No failure preprocessor.** A Revit warning or error dialog blocks the UI thread until a person answers it, and the
  call times out.

## Consequences

`ActionLogger` keeps `transactionMode` in the audit log and replaces the code body with `"<see result.codeFile>"`. The
exact code is in the file the result names ([0027](0027-send-code-scripts-traced-to-local-temp.md); before it,
`"<omitted>"`, per [0023](0023-send-code-scripts-not-persisted.md)).
