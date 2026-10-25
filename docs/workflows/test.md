---
title: Run the tests
type: workflow
updated: 2026-10-24
sources: [server/package.json, server/vitest.config.ts, server/tests/TESTS.md, server/tests/guards/no-acc-writes.test.ts, tests/commandset/CEM_IAModeler_Test.csproj]
related: [../modules/tests.md, ../decisions/0016-schema-only-ts-unit-tests.md, smoke-test.md]
tags: [testing, tdd]
---

# Run the tests

## Fast loop (pure, always run)

```bash
cd server && npx vitest run       # or: npm test
```

From the Cemengal root, the same suite runs with `.\.claude\run-tests.ps1 -Repos revitmcp`.

## TDD here

- New TS tool → write its schema test first ([add a tool](add-a-tool.md) step 1).
- `withRevitConnection` and the socket are a documented gap; don't mock them.
- C# EventHandler logic needs a live `Document`. That is the stated TDD exemption ("Revit-bound").
  Say so in your report rather than skipping silently. The coordinator accepted it again on 2026-10-24 for
  the [0026](../decisions/0026-logs-and-artefacts-in-local-temp.md) handlers.
- **Where C# writes is pinned from the TS side:** `tests/guards/no-acc-writes.test.ts` reads the plugin and
  command-set source and fails when anything writes under ACC, beside the DLL, to the raw `%TEMP%` or the
  Desktop, or when a file not on its inventory writes at all. A new file write means a new inventory line, with
  the reason, and a path from `CemTempFolder` ([0026](../decisions/0026-logs-and-artefacts-in-local-temp.md)).

## Integration tier (manual, needs licensed Revit)

`tests/commandset/` (`Nice3point.TUnit.Revit`, upstream). Run it from the IDE or test runner with
Revit available. It is not part of the fast loop or `run-tests.ps1`.

## Not a test

Driving Revit through the `cem-revit-ai` MCP server while developing is useful, but it is not
coverage. Report "what I drove" and "what is asserted" as separate claims. Never put MCP calls in a
test project. The [smoke test](smoke-test.md) is a manual end-to-end check.

## Never ship tests

Test folders, `vitest.config.ts` and `TESTS.md` must stay out of any package. `server/package.json`
`files` ships only `build/` and `README.md`; the `.mcpb` uses `server/.mcpbignore`.
