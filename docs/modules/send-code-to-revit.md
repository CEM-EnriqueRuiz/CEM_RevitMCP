---
title: send_code_to_revit — the escape hatch
type: module
updated: 2026-11-01
sources: [https://github.com/CEMENGAL/CEM_Workspace/issues/5, server/src/tools/send_code_to_revit.ts, commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, plugin/Utils/ActionLogger.cs, server/src/utils/SocketClient.ts, server/tests/guards/modeller-pins.test.ts]
related: [../decisions/0027-send-code-scripts-traced-to-local-temp.md, ../decisions/0018-send-code-transaction-mode.md, ../concepts/audit-trails.md, ../concepts/cemengal-addins-via-mcp.md, ../decisions/0008-parent-tools-from-recipe-corpus.md, ../decisions/0006-create-methods-not-full-parity.md]
tags: [send-code, roslyn, upstream]
---

# send_code_to_revit — the escape hatch

**Origin: upstream.** Cemengal added recipe logging (`aa88b96`) and later removed it (`fe06b6c`); upstream added
`transactionMode` (`17d0108`); Cemengal added the code trace and the late-completion log (2026-11-01,
[0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md)).

Arbitrary C# is compiled with Roslyn (`Microsoft.CodeAnalysis.CSharp`) into a template
`public static object Execute(Document document, object[] parameters)`. The template has the usings `System`,
`System.Linq`, `Autodesk.Revit.DB`, `Autodesk.Revit.UI` and `System.Collections.Generic`. It runs on the UI thread.

- **The document.** `document` is always the **active** document, `ActiveUIDocument.Document`: the family in the
  Family Editor, or the project. There is no `UIApplication`; build one with `new UIApplication(document.Application)`.
- **Parameters and return.** `parameters` is a `string[]`. The snippet must `return` something, which goes through
  `JsonConvert.SerializeObject`, so return primitives or anonymous objects: Revit objects can loop or throw.
- **References.** The compilation references every assembly loaded in Revit, so a snippet can call any public Cemengal
  add-in class. For the add-ins' jobs there are `cem_*` tools
  ([Cemengal add-ins through the MCP](../concepts/cemengal-addins-via-mcp.md)).

Input: `code` (string), `parameters` (string[], optional), and `transactionMode`: `"auto"`, the default, wraps the
snippet in a transaction, and `"none"` lets the code manage its own
([0018](../decisions/0018-send-code-transaction-mode.md), which has the full table).

## Transactions, rollback and the timeout

- **`auto`** compiles first, then runs the snippet in one transaction on the active document.
  - If the snippet throws, the uncommitted transaction is disposed, which rolls back its changes to that document.
  - If Revit's commit does not return `Committed`, that is also reported as `rolledBack`.
- **`none`** rolls nothing back. Use it for code that opens, saves or creates documents (`NewFamilyDocument`,
  `SaveAs`), and commit your own transactions.
- **No mode covers** other documents, files, or `Save`/`Close`. There is no sandbox.
- **No failure preprocessor.** A Revit warning or error dialog blocks the UI thread until a person answers it, and the
  call times out.
- **The timeout.** The plugin waits 60 s (`ExecuteCodeCommand.TimeoutMs`), the TS client 120 s, and the connect timeout
  is 5 s. Calls are serialised by the server's mutex. At 60 s the snippet is **not stopped**:
  - if it had started, it keeps running and may go on changing the model. The error says so, and the real outcome is
    appended to the action log as a `late` line when it ends;
  - if Revit had not started it (busy, or a dialog open), it never runs.
- **Errors.** They come back in Chinese (`执行失败`, `代码编译错误` with line numbers), now followed by an English note on
  what was rolled back.
  - Compile-error line numbers count the wrapper template: the snippet's first line is reported as line 14, so
    subtract 13.
  - Roslyn's messages follow Revit's UI language, so they are Spanish on this machine. A snippet's exception is unwrapped from the reflection wrapper.
- **Memory.** Each call compiles a new assembly and loads it with `Assembly.Load(byte[])`, which net48 never unloads,
  so memory grows over a long session.

## What it records ([0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md))

- **The exact code** goes to `%TEMP%\Cemengal\RevitMCP\code\send_code_<stamp>_<runId>.cs`. It is written before the
  run, fail-soft, and pruned after 7 days. Never on ACC.
- **The result**, as the TS tool returns it and the action log records it, has:
  - `success`, `result` and `errorMessage`;
  - `outcome`: `ok`, `error` or `rolledBack`;
  - `runId` and `codeFile`;
  - `transactionMode`;
  - `documentTitle` and `isFamilyDocument`;
  - `durationMs`.
- **The action log** (`actions-yyyyMMdd.jsonl`) keeps the call's line, with `code`/`data` as
  `"<see result.codeFile>"`. A run that outlives the timeout adds a second line, flagged `"late": true`, with its real
  outcome and duration.

Before 0027 the code was not kept anywhere: the `Recipes\` dump on ACC was removed in `fe06b6c`, and the log said
`"<omitted>"` ([0023](../decisions/0023-send-code-scripts-not-persisted.md)).

## When to use it

- The long tail that the toolset deliberately doesn't cover: conceptual mass, adaptive components,
  site, rebar/loads/analytical, scope boxes, blends
  ([0006](../decisions/0006-create-methods-not-full-parity.md)).
- **Not** as the default. Repeated snippets are the signal to build a real tool
  ([0008](../decisions/0008-parent-tools-from-recipe-corpus.md)).
