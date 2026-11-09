---
title: "CEMENGAL/CEM_AutocadAPI#4: the log escaper moves to CEM_CSharpExtended; here, LogText onto EscapeForLog"
type: source
updated: 2026-11-09
sources: [https://github.com/CEMENGAL/CEM_AutocadAPI/issues/4, "the user, 2026-11-09 («Sigue con la parte 2»; the architect's recommendations approved; for LogText: null → \"\", lower-case hex, cut optional with no cut by default)", "the user, 2026-11-09 («Commit y push con el #4, sin despliegue»)", "the QA and security reviews of #4 part 2 (full, 0 blocking)", plugin/Utils/Logger.cs, plugin/Core/SocketService.cs, server/tests/plugin/runtime.test.ts, server/tests/fixtures/plugin-runtime.ps1, server/tests/guards/socket-auth-pins.test.ts]
related: [../../decisions/0022-local-shared-secret-handshake.md, ../../concepts/audit-trails.md, ../../modules/plugin.md, ../../modules/tests.md, CEMENGAL-CEM_Workspace-5.md, CEMENGAL-CEM_CSharpExtended-1.md]
tags: [issue, cem_csharpextended, logging, security, refactor, cross-repo]
---
# CEM_AutocadAPI#4: LogText onto CEM_CSharpExtended's EscapeForLog

**Issue:** https://github.com/CEMENGAL/CEM_AutocadAPI/issues/4. It is a cross-repo change to a shared library's API
(level Major, three repos). Part 2 moves the log escaper into CEM_CSharpExtended. This page covers this repo's step
(step B) only.

## What changed here

CEM_CSharpExtended gained `XStringExtensions.EscapeForLog(this string? text, int maxLength = int.MaxValue)`, which
replaces two copies: CEM_AutocadAPI_Extended's `Hosting/LogText.cs` and this repo's `plugin/Utils/LogText.cs` (written
for SEC-5, [0022](../../decisions/0022-local-shared-secret-handshake.md), CWE-117).
- `plugin/Utils/LogText.cs` is deleted.
- `SocketService.Shorten` calls `method.EscapeForLog()`. Its own rules are unchanged: `(none)` for a null method,
  then the 80-character cut after escaping.
- `Logger` holds `EscapeArgs` as its own `private static` policy, built on `EscapeForLog`, with the same rule as before:
  a null argument stays null; an `IFormattable` (numbers, dates, enums) is untouched, so `{0:N1}` still formats;
  everything else is escaped through its text, uncut.

### LogText's old behaviours

| Behaviour | Now |
|---|---|
| CR, LF, TAB → `\r`, `\n`, `\t` | kept |
| the other C0 controls, DEL, the C1 controls, U+2028/U+2029 → `\u` + four hex digits | kept; **the hex is lower-case** (`\u001b`, was `\u001B`) |
| a null text → `(null)` | **now `""`**. Only reachable through an argument whose `ToString()` returns null: `Shorten` handles a null method itself |
| no cut | kept (the default `maxLength`) |
| idempotent; every other character (accents, surrogates) unchanged | kept |
| `EscapeArgs`: a null array → null; a null element stays null; `IFormattable` untouched; the rest escaped through its text | kept, inside `Logger` |
| the public `CEM_IAModeler.Utils.LogText` type | removed |

## Decisions and why

- **The two behaviour changes are the user's** (2026-11-09): a null text escapes to `""`, and the hex is lower-case.
  These follow CEM_AutocadAPI's copy. The cut is optional and off by default, which keeps this repo's no-cut log. Rejected: keeping
  `(null)` and upper-case here, which would need a second escaper or a parameter only this repo uses.
- **`EscapeArgs` stays in the Logger, private.** Which arguments are escaped is the Logger's policy, not a string
  helper. Rejected: an `object[]` extension in CEM_CSharpExtended, a reusable API that no other repo needs.
- **`Shorten` is left as it is** (the coordinator's decision). It is not part of the contract. `EscapeForLog(80)` would
  cut before escaping instead of after, so it would change the logged text. Rejected for that reason.
- **A clean cutover** (root 0041, the C# procedure): no `[Obsolete]` forwarder, and a pin fails if `LogText` comes back
  in `plugin/` or `commandset/`.

## Tests

- Written first; 8 failed before the C# change.
- `tests/plugin/runtime.test.ts` (8, was 6) loads the built DLL. It checks:
  - the `LogText` type is gone;
  - the Logger's private `EscapeArgs` leaves no control character or line separator, in lower-case hex, idempotent,
    with ordinary text kept whole;
  - a null argument stays null, and a text that is null becomes `""`;
  - a char argument is escaped through its text, and numbers and dates stay formattable.
- `tests/guards/socket-auth-pins.test.ts` (24, was 23):
  - the Logger pin and the `Shorten` pin now point at `EscapeForLog`;
  - one new pin: no `LogText` file or reference is left.
- Mutation check: without `.EscapeForLog()` in `EscapeArgs`, 4 tests fail.
- The full suite: 148 tests in 15 files.
- The plugin built as Debug R24 against CEM_CSharpExtended's working tree, with 0 errors.
- No Revit session and no deploy (the user: «sin despliegue»).

## Reviews

QA (full) and security (full): 0 blocking.
- QA-1 and QA-2: Info.
- Security I-1 to I-4: Info. The gaps in the official tests are covered by each repo's own suites.
- Two older known gaps the reviewers found were recorded for their owners, not changed here. Neither is in this repo.
  - F1: two IA Modeler catches reach `CemToolLog.FilePath`, which calls the library outside a try, as `ToolLog.FilePath`
    did.
  - `CemWildcardPattern`'s `Regex` has no timeout.

## Doc drift fixed

[Build and deploy](../../workflows/build-and-deploy.md) said to use the provisioned
`%LOCALAPPDATA%\Cemengal\dotnet\dotnet.exe`. This machine does not have it, and SDK 10.0.301 from PATH built the
plugin. The page now records the SDK as machine-specific: check `dotnet --version` first.

## Push order

This commit compiles against `EscapeForLog`, so CEM_CSharpExtended reaches origin first. This repo's commit is pushed
after it.
