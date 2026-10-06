---
title: "CEM-EnriqueRuiz/CEM_API#50 Implement TDD system methodology"
type: source
updated: 2026-10-06
sources: ["https://github.com/CEM-EnriqueRuiz/CEM_API/issues/50", 0556fb0]
related: [../../decisions/0016-schema-only-ts-unit-tests.md, ../../modules/tests.md, ../../workflows/test.md]
tags: [issue, testing, cross-repo]
---

# CEM-EnriqueRuiz/CEM_API#50 Implement TDD system methodology

- **Tracker:** CEM_API. This is a **cross-repo umbrella issue**: the same URL is carried by commits
  in every repo that got a test suite ([CROSS_REPO.md §4](../../../../../CROSS_REPO.md)).
- **State:** closed. Created 2026-08-28 10:11 UTC, closed 2026-08-28 10:43 UTC.
- **Body:** empty. **Comments:** none. Fetched 2026-10-06 with `gh issue view`.
- Cross-repo view (what every repo did): root wiki
  [features/CEM-EnriqueRuiz-CEM_API-50](../../../../../docs/features/CEM-EnriqueRuiz-CEM_API-50.md).

## What was asked

Only the title: "Implement TDD system methodology". The discussion and plan are not recorded in the
issue. What was done is documented by the commits and by the root TDD reference
(`.claude/skills/cemengal-architect/reference/tdd-and-test-runner.md` in the Cemengal root).

## Commits in this repo

| SHA | Date | Subject |
|---|---|---|
| `0556fb0` | 2026-08-28 | FEAT - Add unit-test system and TDD workflow. https://github.com/CEM-EnriqueRuiz/CEM_API/issues/50 |

`0556fb0` touched: `server/package.json` (`test` script, `vitest` devDependency),
`server/vitest.config.ts`, `server/tests/helpers/captureToolSchema.ts`, five
`server/tests/tools/*.test.ts`, `server/tests/TESTS.md`, and the skill (TDD paragraph and verify
section).

## Decisions extracted

- **Schema-only TS unit suite.** Test each tool's real zod schema through a capturing `McpServer`
  stub, one representative tool per domain. *Rejected:* mocking the socket, which would be heavier
  than the code under test. → [0016](../../decisions/0016-schema-only-ts-unit-tests.md)
- **The C# TUnit suite stays a separate, manual tier** (needs live Revit) and is not in the fast
  loop. Reason: no licensed Revit in the loop. → [tests](../../modules/tests.md)
- **TDD exemption for Revit-bound code** must be stated, not skipped silently. → [test](../../workflows/test.md)
- While writing the tests, the agent found **doc drift**: the skill's "deliberately kept" SQLite
  pack had been removed. This was recorded in `TESTS.md` and is now in
  [0012](../../decisions/0012-drop-sqlite-store-pack.md) and [doc drift](../../concepts/doc-drift.md).
  (`TESTS.md`'s own claim that no Cemengal commit touched `server/src/tools` is wrong; see the same
  page.)

## Pages shaped

[0016](../../decisions/0016-schema-only-ts-unit-tests.md), [tests](../../modules/tests.md),
[test workflow](../../workflows/test.md), [add a tool](../../workflows/add-a-tool.md) step 1.
