---
title: send_code_to_revit — the escape hatch
type: module
updated: 2026-10-07
sources: [server/src/tools/send_code_to_revit.ts, commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs]
related: [../decisions/0018-send-code-transaction-mode.md, ../concepts/audit-trails.md, ../concepts/cemengal-addins-via-mcp.md, ../decisions/0008-parent-tools-from-recipe-corpus.md, ../decisions/0006-create-methods-not-full-parity.md]
tags: [send-code, roslyn, upstream]
---

# send_code_to_revit — the escape hatch

**Origin: upstream.** Cemengal added recipe logging (`aa88b96`) and later removed it (`fe06b6c`). Upstream added `transactionMode`
(`17d0108`).

Arbitrary C# is compiled with Roslyn (`Microsoft.CodeAnalysis.CSharp`) into a template `Execute`
method that has `Document` and `parameters`, then runs on the UI thread. The compilation references
every assembly loaded in Revit, so a snippet can call any public Cemengal add-in class; for the add-ins'
jobs there are `cem_*` tools ([Cemengal add-ins through the MCP](../concepts/cemengal-addins-via-mcp.md)).

Input: `code` (string), `parameters` (string[], optional), and `transactionMode` (`"auto"`, the
default, wraps the snippet in a transaction; `"none"` lets the code manage its own,
[0018](../decisions/0018-send-code-transaction-mode.md)).

Scripts are **not persisted**. The per-call `Recipes\` dump (added in `aa88b96`) was removed in `fe06b6c`,
and the action log records the call with `code`/`data` as `"<omitted>"`
([0023](../decisions/0023-send-code-scripts-not-persisted.md), [audit trails](../concepts/audit-trails.md)).

## When to use it

- The long tail that the toolset deliberately doesn't cover: conceptual mass, adaptive components,
  site, rebar/loads/analytical, scope boxes, blends
  ([0006](../decisions/0006-create-methods-not-full-parity.md)).
- **Not** as the default. Repeated snippets are the signal to build a real tool
  ([0008](../decisions/0008-parent-tools-from-recipe-corpus.md)).
