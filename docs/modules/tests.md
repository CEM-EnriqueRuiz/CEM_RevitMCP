---
title: Test suites (server/tests and tests/commandset)
type: module
updated: 2026-10-06
sources: [server/tests/TESTS.md, server/tests/helpers/captureToolSchema.ts, server/vitest.config.ts, tests/commandset/CEM_IAModeler_Test.csproj, ea54485, 0556fb0]
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
- `TESTS.md`: the traceability note. Add one line per new test. (Its "Traceability" paragraph has a
  factual error; see [doc drift](../concepts/doc-drift.md).)

## tests/commandset

Covers upstream commands only: level and room creation, room tagging, color splash, model
statistics, room data export, material quantities. It is not run by the root `run-tests.ps1`.
