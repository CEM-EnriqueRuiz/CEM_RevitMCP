---
title: send_code_to_revit — the escape hatch
type: module
updated: 2026-10-06
sources: [server/src/tools/send_code_to_revit.ts, commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs]
related: [../decisions/0018-send-code-transaction-mode.md, ../concepts/audit-trails.md, ../decisions/0008-parent-tools-from-recipe-corpus.md, ../decisions/0006-create-methods-not-full-parity.md]
tags: [send-code, roslyn, upstream]
---

# send_code_to_revit — the escape hatch

**Origin: upstream.** Cemengal added recipe logging (`aa88b96`). Upstream added `transactionMode`
(`17d0108`).

Arbitrary C# is compiled with Roslyn (`Microsoft.CodeAnalysis.CSharp`) into a template `Execute`
method that has `Document` and `parameters`, then runs on the UI thread.

Input: `code` (string), `parameters` (string[], optional), and `transactionMode` (`"auto"`, the
default, wraps the snippet in a transaction; `"none"` lets the code manage its own,
[0018](../decisions/0018-send-code-transaction-mode.md)).

Every script is saved to the `Recipes\` audit folder **before** execution
([audit trails](../concepts/audit-trails.md)).

## When to use it

- The long tail that the toolset deliberately doesn't cover: conceptual mass, adaptive components,
  site, rebar/loads/analytical, scope boxes, blends
  ([0006](../decisions/0006-create-methods-not-full-parity.md)).
- **Not** as the default. Repeated recipes are the signal to build a real tool
  ([0008](../decisions/0008-parent-tools-from-recipe-corpus.md)).
