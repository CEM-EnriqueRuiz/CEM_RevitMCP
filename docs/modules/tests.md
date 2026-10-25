---
title: Test suites (server/tests and tests/commandset)
type: module
updated: 2026-10-24
sources: [server/tests/TESTS.md, server/tests/guards/no-acc-writes.test.ts, server/tests/helpers/captureToolSchema.ts, server/vitest.config.ts, tests/commandset/CEM_IAModeler_Test.csproj, ea54485, 0556fb0]
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
  `viz_create_material`, one per representative domain.
- `utils/connection.test.ts`: the client is pinned to `127.0.0.1:8080` and reaches an IPv4-loopback-only
  listener ([0021](../decisions/0021-loopback-only-socket.md)). The second test skips if port 8080 is busy.
- `guards/no-acc-writes.test.ts`: the root-0031 guard ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)).
  It reads the C# source of `plugin/` and `commandset/` (and `server/src`) and fails on an ACC path, a write
  beside the DLL or to the raw temp/Desktop, a Node file write, or a C# writer missing from its inventory. It
  also pins the moved logs, the screenshot kind, the shared parameter restore, the no-in-place-save-on-ACC rule
  (families and team shared parameter files) and the `CEM_CSharpExtended` ProjectReferences. 16 tests: 14 red
  first (the Node server never wrote a file); the shared-parameter pin, added with its fix, was shown red by
  mutation. All mutation-checked.
- `TESTS.md`: the traceability note. Add one line per new test. (Its "Traceability" paragraph has a
  factual error; see [doc drift](../concepts/doc-drift.md).)

## tests/commandset

Covers upstream commands only: level and room creation, room tagging, color splash, model
statistics, room data export, material quantities. It is not run by the root `run-tests.ps1`.
