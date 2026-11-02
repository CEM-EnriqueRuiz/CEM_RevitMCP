---
title: "CEMENGAL/CEM_Workspace#5: the Revit MCP made ready for the modelling agent (family activation, send_code trace, export_view_image)"
type: source
updated: 2026-11-02
sources: [https://github.com/CEMENGAL/CEM_Workspace/issues/5, "the user, 2026-11-01 («1. ok», «sí, que instale y abra Revit», «Sí, dale permiso»)", commandset/Services/Family/FamilySessionEventHandlers.cs, commandset/Models/Family/FamilyGeomModels.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, commandset/Commands/Views/ExportViewImageCommand.cs, commandset/Services/Views/ExportViewImageEventHandler.cs, commandset/Models/Views/ExportViewImageModels.cs, commandset/Services/Core/CreateSharedParameterEventHandler.cs, commandset/Utils/McpLocalFiles.cs, plugin/Utils/ActionLogger.cs, command.json, server/src/tools/export_view_image.ts, server/src/tools/send_code_to_revit.ts, server/src/tools/family_open_session.ts, server/tests/guards/modeller-pins.test.ts, server/tests/guards/no-acc-writes.test.ts, server/tests/tools/export_view_image.test.ts, server/tests/tools/family_open_session.test.ts, "the user, 2026-11-01/02 on SEC-5 («Con la SEG5 hacemos lo que recomiendas», «respecto a SEC-5 hacemos lo que propones», «2. sí»)", plugin/Security/McpAuthToken.cs, plugin/Core/SocketService.cs, plugin/Utils/LogText.cs, server/src/utils/SocketClient.ts]
related: [../../decisions/0022-local-shared-secret-handshake.md, ../../decisions/0027-send-code-scripts-traced-to-local-temp.md, ../../decisions/0023-send-code-scripts-not-persisted.md, ../../decisions/0026-logs-and-artefacts-in-local-temp.md, ../../decisions/0018-send-code-transaction-mode.md, ../../modules/family-editing.md, ../../modules/send-code-to-revit.md, ../../modules/toolset.md, ../../concepts/audit-trails.md, ../../workflows/smoke-test.md]
tags: [issue, family, send-code, export, audit, security, modeller]
---
# CEM_Workspace#5: the Revit MCP made ready for the modelling agent

**Issue:** https://github.com/CEMENGAL/CEM_Workspace/issues/5. It is a cross-repo umbrella for the 2026-11-01 work
(CEM_RevitMCP, CEM_AutocadMCP, CEM_CSharpExtended, CEM_NavisworksMCP and the reviewers); this page covers this repo.

## What was asked

The planned modelling agent (CEM_Modeler) will build Revit equipment families, mostly through `send_code_to_revit`,
in the Family Editor, and will check its work visually one view at a time. A read-only supervisor will review it.
This repo's inventory of 2026-11-01 found three gaps, and the user approved fixing them («1. ok»):

1. `family_open_session` never activated the family it opened, so the `family_*` tools, which act on the active
   document, failed right after it.
2. Nothing recorded what `send_code_to_revit` ran. A snippet still running at the timeout was logged as failed while
   it went on changing the model.
3. There was no per-view image export; `take_screenshot` grabs the whole Revit window.

Out of scope: the supervisor's read-only profile, role tokens, `family_inspect`, and any other family tool.

## Decisions and why

- **The exact code is traced** to `%TEMP%\Cemengal\RevitMCP\code\send_code_<stamp>_<runId>.cs`. This is
  [0027](../../decisions/0027-send-code-scripts-traced-to-local-temp.md), which supersedes
  [0023](../../decisions/0023-send-code-scripts-not-persisted.md)'s "scripts are not persisted".
  - **0023's reason was** that `Recipes\` was noise, on ACC and never cleaned.
  - **Why the reason no longer holds:** the new files are local, per user and pruned after 7 days.
  - **Why it is needed now:** the modeller and its supervisor need to know what ran.
  - **Rejected:** the code in the JSONL line (truncated, and it drowns the log); `Recipes\` on ACC (root 0031);
    killing a snippet at the timeout (no safe way on the UI thread).
- **The result and the log line carry:** the outcome (`ok`, `error` or `rolledBack`), the mode, the active
  document, the duration and the code file.
- **A run that outlives its 60 s timeout** adds a `"late": true` line with its real outcome.
- **A run abandoned before it started never runs,** and gets one "never ran" line. Whoever reaches it first writes
  that line: the handler, or the next call that replaces it.
- **Activation goes through `OpenAndActivateDocument`.** Revit activates only a document with a file, so a pathless
  `EditFamily` family is first saved to a temp copy. The coordinator accepted this, on condition that the tool
  description says so.
- **Closing the active family:** Revit cannot close the active document, so `family_save_session` with `close` first
  re-activates the project.
- **`export_view_image`** is a Core flat name in the `Views` folder.
  - The detail level is set in a `TransactionGroup` that is always rolled back.
  - Each export writes a new PNG in `images\`.
  - The tool is tagged `"access": "read"` in `command.json`, the first access tag, for the coming supervisor profile.
- **Tests are source pins.** The C# runs only in Revit ([0016](../../decisions/0016-schema-only-ts-unit-tests.md)),
  so the pins follow the `no-acc-writes` guard style. The coordinator declined a new C# test project.
- **The fix batch (QA and security quick reviews, 0 blocking; the user approved fixing the Medium and Info
  findings):**
  - the family host for a name: `hostDocument`, else the active project, else the open project, else the active
    document;
  - family copies in a new folder each, never overwritten. This amends 0026, which had rejected per-copy folders
    because of the empty folders; those are now swept.
  - path reuse by full path;
  - a safe view name and an `images\` check for export (CWE-22);
  - no wait reset in `export_view_image`;
  - the skills' counts, pinned against the registry.
- **SEC-8 consumer guards:** CEM_CSharpExtended's `CemTempFolder` now refuses reparse points, with
  `CemTempFolderUnsafeException`. This repo refuses them at the paths it derives itself, with
  `McpLocalFiles.RefuseReparsePoint`:
  - the `Families\<name>_<stamp>\` folder, before and after it is created;
  - the fixed shared-parameter file and its folder.
  CEM_CSharpExtended itself was changed by its own agent.

## Findings and their end state

| Finding | End state |
|---|---|
| QA-2 (Medium): a second open by name searched the active family's nested families | fixed: `ResolveHost`, `hostDocument`; verified live |
| QA-7 (Medium, recheck): with two projects open the first project won, not the active one | fixed: the active project first; pinned, not driven live (needs two projects) |
| QA-3 (Info): `export_view_image` kept the wait-reset race | fixed: the request setter re-arms the signal |
| QA-5 (Info): the pathless family copy was overwritten by name | fixed: `Families\<name>_<stamp>\<name>.rfa`; verified live |
| QA-6 (test gap): the detail level only inside the rolled-back group | pinned |
| SEC-2 (Low, CWE-22): a view name with `/` or `..` could put the PNG outside `images\` | fixed: a safe name inside the group plus a `GetFileName` check. Live: Revit 2024 already sanitises the name itself, so this is defence in depth |
| SEC-6 (test gap): the trace id must be generated, never the caller's | pinned; the code already did it |
| Review note: 0027's "never ran" line was lost when a newer call replaced the run | fixed: `TryClaimNeverRan` |
| Review note: `FindOpenFamily` matched by name | fixed: `FindOpenFamilyByPath` |
| Review note: the skills' tool counts disagreed | fixed: 125 MCP tools and 127 commands in both, pinned |
| SEC-8 (CEM_CSharpExtended, consumer side): reparse points under the temp folder | guarded here; not driven live |

Left open:
- The other upstream-pattern handlers still reset the wait signal ([doc drift](../../concepts/doc-drift.md)).
- Re-activating the project before `family_save_session` closes a family is not driven live.

## What changed

| Area | Change |
|---|---|
| `FamilySessionEventHandlers.cs`, `FamilyGeomModels.cs`, `family_open_session.ts` | activation; `hostDocument`/`hostTitle`; `ResolveHost`; `FindOpenFamilyByPath`; `NewFamilyCopyPath` with reparse checks; the close re-activates the project |
| `ExecuteCodeCommand.cs`, `ExecuteCodeEventHandler.cs`, `send_code_to_revit.ts` | the code trace, the result fields, `ExecutionRun` (start, complete, abandon, never-ran), late lines, compile before the transaction, unwrapped exceptions |
| `plugin/Utils/ActionLogger.cs` | `code`/`data` logged as `"<see result.codeFile>"` |
| `ExportViewImageCommand.cs`, `ExportViewImageEventHandler.cs`, `ExportViewImageModels.cs`, `export_view_image.ts`, `command.json` | the new tool (127 commands, 125 TS tools) |
| `CreateSharedParameterEventHandler.cs`, `McpLocalFiles.cs` | the reparse-point guard |
| tests | `guards/modeller-pins.test.ts` (30), `tools/export_view_image.test.ts` (7), `tools/family_open_session.test.ts` (4), and the `no-acc-writes` inventory (two writers). Every new test was red first, except the SEC-6 pin, which locks existing behaviour in. 103 tests pass |
| skills | `cem-revitmcp-dev`: rules 7 and 11, and the counts. `cem-aimodeler`: `export_view_image`, the active family, the Family Editor's halftone, and the counts |
| wiki | new [0027](../../decisions/0027-send-code-scripts-traced-to-local-temp.md); [0023](../../decisions/0023-send-code-scripts-not-persisted.md) superseded; [0026](../../decisions/0026-logs-and-artefacts-in-local-temp.md) amended; [0018](../../decisions/0018-send-code-transaction-mode.md) with the rollback and timeout table; [send code](../../modules/send-code-to-revit.md), [family editing](../../modules/family-editing.md), [toolset](../../modules/toolset.md), [command registry](../../modules/command-registry.md), [audit trails](../../concepts/audit-trails.md), [doc drift](../../concepts/doc-drift.md), [tests](../../modules/tests.md), [smoke test](../../workflows/smoke-test.md), [index](../../index.md) and the counts |

## Verified live (Revit 2024, after a CEM_RibbonUI Debug R24 deploy)

These are two manual checks, not tests. They used new families from the corporate generic-model template, opened
read-only, and saved nothing beyond our own temp copies.

- **Activation:** both open branches made the family active.
- **`send_code_to_revit`:**
  - each outcome came back correctly;
  - a 75 s snippet reported a timeout at 60 s, and its late line came at 75.4 s;
  - each code file held the exact snippet.
- **`export_view_image`:**
  - the document was left unmodified at all three levels;
  - Fine-only geometry came out greyed at Coarse;
  - a view named `a/../../x` landed in `images\`.
- **`hostDocument`:** it worked, and an open by name without it failed clearly.
- **Copies:** each went to its own folder.
- **Nothing was written beside the DLLs.**

The details are in the [smoke test](../../workflows/smoke-test.md).

## SEC-5: the Revit socket's shared secret (the second commit, 2026-11-02)

The security review of this issue found SEC-5: any local account (here, the Codex sandbox) could reach
`127.0.0.1:8080` and run C# through `send_code_to_revit`. The user: «Con la SEG5 hacemos lo que recomiendas». The
contract is [0022](../../decisions/0022-local-shared-secret-handshake.md), now current.

**Decisions and why**

- **A token per listener start,** 32 CSPRNG bytes in `%LOCALAPPDATA%\Cemengal\RevitMCP\token`, readable only by the
  user and SYSTEM. It is issued after the bind, so a second Revit that cannot bind does not replace the first one's
  token, and revoked on `Stop`.
  - Rejected: a fixed secret in config (it never rotates); `%TEMP%` (pruned, shared); named pipes with an ACL (they
    replace upstream's transport on both ends); rotation within a session (per-start rotation covers a restart).
- **`params.auth` on every request,** checked in constant time in `SocketService` before `CommandExecutor`, and
  stripped before the command and the action log see it. A refusal gets -33200 "Unauthorized" with no detail.
  Rejected: a first-line handshake, which changes upstream's framing.
- **The TS client re-reads the file and retries once,** so a Revit or service restart recovers without restarting the
  MCP server. The server must run as the Revit user.
- **Revit's `ApplicationClosing` calls `Stop`** (found live: CEM_RibbonUI has no `OnShutdown`, so the token file
  outlived Revit). Fixed in the plugin, not the host, so no host has to remember it.
- **The fix batch (the user: «respecto a SEC-5 hacemos lo que propones»):**
  - every `Logger` format argument escaped by `LogText` (CWE-117);
  - the token write: a POSIX-semantics rename, then `MoveFileEx` where unsupported, retried for about 1.3 s.
    Rejected: a plain retry (199 of 200 writes still failed under a reading loop) and `ReplaceFile` (the same failure).
  - A cancelled close of Revit still stops the service: recorded as a known limitation, since it fails safe.
- **Batch 2 (the user: «2. sí»):** `ExclusiveAddressUse` before each bind (SEC-5.2, CWE-605), and `CommandManager`'s
  malformed `({1}}` log format fixed, with a guard over every plugin log message.

**Findings and their end state**

| Finding | End state |
|---|---|
| SEC-5 (any local account could call the socket) | fixed: [0022](../../decisions/0022-local-shared-secret-handshake.md); verified live |
| The token file outlived Revit (found live) | fixed: `ApplicationClosing`; proved live by both reviewers (QA L4, security C) |
| SEC-5.1 (Low, CWE-117): a refused method forged `mcp-*.log` lines | fixed: `LogText`; runtime-tested |
| QA-1 (Low): the token write failed while a reader held the file | fixed: POSIX rename and bounded retry; runtime-tested, mutation-checked |
| A cancelled close stops the service | recorded as a known limitation in 0022 |
| SEC-5.2 (Info, CWE-605): the port not held exclusively | fixed: `ExclusiveAddressUse`; pinned |
| `CommandManager`'s `({1}}` format (an older upstream bug) | fixed; `log-format` guard |
| SEC-5.3 (Info, CWE-940): the listener does not prove itself to the client | **open design question** for the user, recorded in 0022 |

**What changed**

| Area | Change |
|---|---|
| `plugin/Security/McpAuthToken.cs` (new), `plugin/Core/SocketService.cs` | the token's issue, check, revoke and write; the closing hook; the exclusive port; the escaped refusal line |
| `plugin/Utils/LogText.cs` (new), `Logger.cs`, `ActionLogger.cs`, `McpLocalFiles.cs`, `ExternalEventManager.cs`, `CommandManager.cs` | escaping; `auth` dropped from the action log; the reparse helpers; no interpolated logger message; the format fix |
| `server/src/utils/SocketClient.ts` | the token sent, re-read and retried once; never in an error |
| tests | `utils/auth.test.ts` (9), `guards/socket-auth-pins.test.ts` (23), `plugin/runtime.test.ts` (6, the built DLL in PowerShell), `guards/log-format.test.ts` (3), `no-acc-writes` and `connection.test.ts` updated. All red first except the noted locks. 145 tests in 15 files pass |
| skills | `cem-revitmcp-dev` rule 14; `cem-aimodeler`: what the token errors mean |
| wiki | [0022](../../decisions/0022-local-shared-secret-handshake.md) current; [0021](../../decisions/0021-loopback-only-socket.md), [0014](../../decisions/0014-license-gated-listener.md), [plugin](../../modules/plugin.md), [server](../../modules/server.md), [tests](../../modules/tests.md), [audit trails](../../concepts/audit-trails.md), [smoke test](../../workflows/smoke-test.md), [test](../../workflows/test.md), [index](../../index.md) |
