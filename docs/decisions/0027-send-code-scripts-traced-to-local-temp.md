---
title: "0027 send_code_to_revit traces the exact code it runs to %TEMP%\\Cemengal\\RevitMCP\\code"
type: decision
status: current
updated: 2026-11-01
sources: [https://github.com/CEMENGAL/CEM_Workspace/issues/5, "the user, 2026-11-01 («1. ok» to the modelling-agent fixes)", commandset/Commands/ExecuteDynamicCode/ExecuteCodeCommand.cs, commandset/Commands/ExecuteDynamicCode/ExecuteCodeEventHandler.cs, plugin/Utils/ActionLogger.cs, server/src/tools/send_code_to_revit.ts, server/tests/guards/modeller-pins.test.ts, server/tests/guards/no-acc-writes.test.ts]
related: [../sources/issues/CEMENGAL-CEM_Workspace-5.md, 0023-send-code-scripts-not-persisted.md, 0026-logs-and-artefacts-in-local-temp.md, 0018-send-code-transaction-mode.md, 0008-parent-tools-from-recipe-corpus.md, ../modules/send-code-to-revit.md, ../concepts/audit-trails.md]
tags: [send-code, audit, logging, transactions]
---

# 0027 send_code_to_revit traces the exact code it runs to %TEMP%\Cemengal\RevitMCP\code

Supersedes the "scripts are not persisted" clause of [0023](0023-send-code-scripts-not-persisted.md). The location
rule of [0026](0026-logs-and-artefacts-in-local-temp.md) (root 0031) is unchanged and applies to the new files.

## Context

- **0023's reason (2026-10-05).** The per-call `Recipes\` dump lived on ACC (`…\05-CEMAIModeler\Recipes\`, synced to
  the whole team), grew without limit, and the user called it "noise". It was removed, and the action log kept
  `code`/`data` as `"<omitted>"`.
- **What changed (2026-11-01).** A planned modelling agent, CEM_Modeler, will build equipment families mostly through
  `send_code_to_revit`, and a read-only supervisor will check its work. The inventory found that afterwards nobody can
  tell what code ran: the log said only that a snippet was sent. It also found that a snippet still running at the
  60 s timeout was logged as failed while it went on changing the model unseen. The user approved tracing the executed
  code («1. ok»).

**Why traceability now outweighs the noise.** The two objections to `Recipes\` no longer hold:
- It was on ACC. The new files live only in the user's own `%TEMP%\Cemengal\RevitMCP\code`, local and never synced.
- It was never cleaned up. The folder is pruned with the rest: files older than 7 days go, and the app folder stays
  under 50 MB.

What is left is a per-machine trace that an agent session, its supervisor or a person can read when a model looks
wrong.

## Decision

- **The exact code is written before it runs.** `ExecuteCodeCommand` writes `code` verbatim, not the wrapped template,
  before it raises the event. The file is `code\send_code_<yyyyMMdd_HHmmss_fff>_<runId>.cs`: the stamp comes from
  `CemTempFolder.NewArtifactFile`, and `runId` is 8 hex characters per call. UTF-8, no BOM. The write is fail-soft: if
  it fails, `codeFile` is null and the code still runs.
- **The result carries the trace.** On top of `success`, `result` and `errorMessage`, the result has:
  - `runId` and `codeFile`;
  - `transactionMode`;
  - `documentTitle` and `isFamilyDocument`: the active document the snippet ran on;
  - `outcome`, one of:
    - `ok`;
    - `error`: it did not compile, or it threw with nothing to roll back (always the case in `none` mode);
    - `rolledBack`: it threw inside the `auto` transaction, or Revit's commit did not return `Committed`;
  - `durationMs`, compile included.
  `CommandExecutor` logs that result in the call's normal `actions-*.jsonl` line, and `ActionLogger` replaces the
  `code`/`data` params with `"<see result.codeFile>"`. The body stays out of the JSONL, which keeps the log small.
- **A run that outlives its timeout says so.** Each call is an `ExecutionRun` with one lock over three states:
  started, completed and abandoned.
  - When the 60 s wait expires and the run has not completed, the command abandons it and throws. The message names the
    run and the code file, and says whether the code is still running.
  - A run abandoned **before Revit started it** (Revit busy, or a dialog open) never runs, and gets a line saying so.
    - If its event fires, the handler writes that line.
    - If the next call replaces it first, its event runs the new call, and the next call writes it.
    - `ExecutionRun.TryClaimNeverRan` lets only the first of the two write it. Corrected on 2026-11-01: until then the
      line was lost when a newer call replaced the run.
  - A run abandoned **while running** finishes, and the handler appends its own line to the same day's
    `actions-*.jsonl`: `{ts, tool, input:{codeFile, transactionMode}, ok, result, late:true}`. The result note gives
    how long it ran.
  - A run that completes in the race with the timeout returns its result normally.
  So the log never leaves "timed out" as the last word on code that went on changing the model.
- **No lost completions.** `WaitForCompletion` waits on the run's own event and never calls `Reset()`. The upstream
  pattern did, so a completion that landed before the wait began was reported as a timeout.
- **The rollback semantics are documented and pinned:**
  - `auto` disposes the uncommitted transaction on an exception, which rolls back the snippet's changes to the active
    document only;
  - `none` rolls nothing back;
  - neither covers other documents, files or `Save` calls.
  These are documented in [0018](0018-send-code-transaction-mode.md) and [the module page](../modules/send-code-to-revit.md).

## Rejected alternatives

- **The code in the JSONL line.** A long snippet is truncated at 8,000 characters along with the result, and it would
  drown the one-line-per-call log that sessions are evaluated from.
- **Reviving `Recipes\` on ACC.** Root 0031 forbids it, and the user rejected it once as noise.
- **Writing the file in the handler.** The command writes it, so a run that never starts (abandoned while Revit is
  busy) still has its code on disk, and the timeout message can name the file.
- **Killing a snippet at the timeout.** There is no safe way to abort managed code on Revit's UI thread. The call
  reports honestly instead.

## Consequences

- `code\` joins `Screenshots\`, `SharedParameters\`, `Families\` and `images\` in the
  [audit trails](../concepts/audit-trails.md). `ExecuteCodeCommand.cs` is on the writer inventory of
  `server/tests/guards/no-acc-writes.test.ts`.
- `server/tests/guards/modeller-pins.test.ts` pins:
  - the code write before the raise;
  - the abandon on timeout and the late line;
  - the result fields, and the commit status check;
  - no `Reset()` in the wait;
  - the log pointer.
- The recipe corpus of [0008](0008-parent-tools-from-recipe-corpus.md) can grow again, per machine and for 7 days.
  The evidence for the next tool is still how often `send_code_to_revit` appears, and now also what it ran.
- The code is the caller's: it may hold model data or paths, and it stays on the user's machine, in their temp
  folder.
- Verified live on 2026-11-01 (Revit 2024): the outcomes, the code files, and a 75 s snippet whose `late` line came at
  75.4 s ([smoke test](../workflows/smoke-test.md#live-check-2026-11-01-revit-2024-after-a-cem_ribbonui-debug-r24-deploy)).
