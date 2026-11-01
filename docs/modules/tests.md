---
title: Test suites (server/tests and tests/commandset)
type: module
updated: 2026-11-01
sources: [server/tests/TESTS.md, server/tests/guards/no-acc-writes.test.ts, server/tests/guards/modeller-pins.test.ts, server/tests/tools/export_view_image.test.ts, server/tests/helpers/captureToolSchema.ts, server/vitest.config.ts, tests/commandset/CEM_IAModeler_Test.csproj, ea54485, 0556fb0]
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
  listener ([0021](../decisions/0021-loopback-only-socket.md)). The second test skips if port 8080 is busy.
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
  The suite is 103 tests in 11 files.
- `TESTS.md`: the traceability note. Add one line per new test. (Its "Traceability" paragraph has a
  factual error; see [doc drift](../concepts/doc-drift.md).)

## tests/commandset

Covers upstream commands only: level and room creation, room tagging, color splash, model
statistics, room data export, material quantities. It is not run by the root `run-tests.ps1`.
