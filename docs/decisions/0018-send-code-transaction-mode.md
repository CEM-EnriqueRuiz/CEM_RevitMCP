---
title: "0018 send_code_to_revit takes a transactionMode (auto | none)"
type: decision
status: current
updated: 2026-10-06
sources: [17d0108, 86cf705, server/src/tools/send_code_to_revit.ts, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs]
related: [../modules/send-code-to-revit.md]
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

## Consequences

`ActionLogger` keeps `transactionMode` in the audit log and replaces only the code body with a
pointer ([0013](0013-hardcoded-failsoft-audit-trails.md)).
