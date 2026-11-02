---
title: Test suites (server/tests and tests/commandset)
type: module
updated: 2026-11-02
sources: [server/tests/TESTS.md, server/tests/guards/no-acc-writes.test.ts, server/tests/guards/modeller-pins.test.ts, server/tests/guards/socket-auth-pins.test.ts, server/tests/guards/log-format.test.ts, server/tests/utils/auth.test.ts, server/tests/tools/export_view_image.test.ts, server/tests/helpers/captureToolSchema.ts, server/vitest.config.ts, tests/commandset/CEM_IAModeler_Test.csproj, ea54485, 0556fb0]
related: [../decisions/0016-schema-only-ts-unit-tests.md, ../workflows/test.md]
tags: [testing]
---

# Test suites (server/tests and tests/commandset)

| Suite | Path | Framework | Origin | Needs |
|---|---|---|---|---|
| TS schema unit tests | `server/tests/` | vitest | Cemengal, `0556fb0` ([CEM_API#50](../sources/issues/CEM-EnriqueRuiz-CEM_API-50.md)) | nothing (pure) |
| C# integration tests | `tests/commandset/` (`CEM_IAModeler_Test.csproj`, namespace `CEM_IAModeler_Test`) | `Nice3point.TUnit.Revit` | upstream, `ea54485` (40 tests) | a live, licensed Revit |

## server/tests

- `vitest.config.ts`: node environment, `tests/**/*.test.ts`.
- `helpers/captureToolSchema.ts`: a stub `McpServer` that captures `server.tool(...)` and returns
  `z.object(shape)` from the real tool file.
- `tools/`: `create_level`, `family_add_parameter`, `arch_create_wall`, `mep_create_duct`,
  `viz_create_material`, one per representative domain; `cem_tools`; `export_view_image` and `family_open_session` (2026-11-01).
- `utils/connection.test.ts`: the client is pinned to `127.0.0.1:8080` and reaches an IPv4-loopback-only
  listener ([0021](../decisions/0021-loopback-only-socket.md)). The second test skips if port 8080 is busy; it
  points `LOCALAPPDATA` at a scratch token, since every request carries one.
- `utils/auth.test.ts` (2026-11-01, 9 tests): the real `RevitClientConnection` against a fake plugin on an ephemeral
  port ([0022](../decisions/0022-local-shared-secret-handshake.md)). It covers:
  - the token sent with every request;
  - a restart re-read and retried once;
  - a wrong token refused after one retry;
  - a missing or empty file that sends nothing and says why;
  - the token never in an error or on the console;
  - the 120 s timeout, not retried;
  - the default path.
  8 were red first. The timeout test already passed and now locks that path in.
- `guards/socket-auth-pins.test.ts` (2026-11-01, 22 tests): the plugin's side of 0022, read from the source. It pins
  the CSPRNG, the path, the user-and-SYSTEM ACL, the owner check, the temp file then the move, the reparse checks,
  Revit's `ApplicationClosing` calling `Stop` (two pins, red first, added after the live check), the replace with its
  POSIX rename and bounded retry, the escaped log arguments with no interpolated logger message (three pins, red first,
  2026-11-02), the exclusive port set before each bind (SEC-5.2, red first, 2026-11-02; 23 tests now), the
  check before `CommandExecutor`, the constant-time compare, the stripped param, and no token variable in any log,
  trace or exception. 14 of the first 16 were red first. The loopback pin and the no-leak scan already passed (the
  scan was mutation-checked against sample leaks), and the owner pin came with its code.
- `plugin/runtime.test.ts` (2026-11-02, 6 tests): plugin code that never touches Revit, run for real. It loads the
  built `plugin\bin\Debug\2024\CEM_IAModeler.dll` into Windows PowerShell (`tests/fixtures/plugin-runtime.ps1`).
  - `LogText` escapes caller text (0022, CWE-117).
  - 200 `McpAuthToken` writes into a scratch folder all succeed under two readers: the TS client's `readFileSync` in
    a Node loop (`tests/fixtures/token-reader.cjs`), and a scanner-like reader that does not share delete. No read is
    partial, and the file never vanishes.
  - It skips when the DLL is not built and fails when it is older than its sources: rebuild plugin Debug R24 first.
  - Red first: `LogText` did not exist, and 199 of 200 writes failed with Win32 error 5. The atomicity test already
    passed and locks that in. Both halves of the fix were mutation-checked (no POSIX rename, or no retry: red).
- `guards/log-format.test.ts` (2026-11-02, 3 tests): every `_logger` message in `plugin/` is a valid .NET composite
  format string for its argument count (`{{`/`}}`, `{index[,alignment][:format]}`, no lone brace), read from the source.
  Red first on `CommandManager`'s `({1}}`, which would have thrown `FormatException`
  ([0022](../decisions/0022-local-shared-secret-handshake.md)).
- `guards/no-acc-writes.test.ts`: the root-0031 guard ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)).
  It reads the C# source of `plugin/` and `commandset/` (and `server/src`) and fails on an ACC path, a write
  beside the DLL or to the raw temp/Desktop, a Node file write, or a C# writer missing from its inventory. It
  also pins the moved logs, the screenshot kind, the shared parameter restore, the no-in-place-save-on-ACC rule
  (families and team shared parameter files) and the `CEM_CSharpExtended` ProjectReferences. 16 tests: 14 red
  first (the Node server never wrote a file); the shared-parameter pin, added with its fix, was shown red by
  mutation. All mutation-checked.
- `guards/modeller-pins.test.ts` (2026-11-01): the same source-reading style, for the modelling agent's three fixes:
  - `family_open_session` activates the family, and `family_save_session` re-activates the project before closing
    ([family editing](family-editing.md));
  - the `send_code_to_revit` code trace, the timed-out run abandoned and logged `late`, and the result fields
    ([0027](../decisions/0027-send-code-scripts-traced-to-local-temp.md));
  - `export_view_image`'s always-rolled-back group, its `images\` file and its `access: read` tag.
  14 tests, all red first. The review fix batch (2026-11-01) added 10 more:
  - the QA-2 host and the QA-5 never-overwritten copies;
  - the reuse of an open document by path;
  - SEC-2: a safe name, and the file directly in `images\`;
  - QA-3: no `Reset()` in the wait;
  - QA-6: the detail level and the name change only inside the rolled-back group;
  - SEC-6: the trace id is generated, never the caller's;
  - the "never ran" claim;
  - the skills' counts against the registry.
  `tools/family_open_session.test.ts` (4, `hostDocument`) came with them. The QA recheck added 2 more for QA-7: the
  active project comes first, and the tool description gives that order. The SEC-8 follow-up added 4 more, all red first:
  - the reparse-point helper;
  - the copy folder, checked before and after it is created;
  - `Activate`'s clear error;
  - the shared-parameter file, checked before and after the write.
  The suite was 103 tests in 11 files. With 0022's two files, and the token writer's pin in `no-acc-writes` (17
  tests now), it is 132 tests in 13 files. With the fix batch of 2026-11-02 (the runtime test, and three pins), 141 tests in 14 files. With the
  second batch (the exclusive-port pin and `log-format`), 145 tests in 15 files.
- `TESTS.md`: the traceability note. Add one line per new test. (Its "Traceability" paragraph has a
  factual error; see [doc drift](../concepts/doc-drift.md).)

## tests/commandset

Covers upstream commands only: level and room creation, room tagging, color splash, model
statistics, room data export, material quantities. It is not run by the root `run-tests.ps1`.
