---
title: "0005 AI-grade tool conventions"
type: decision
status: current
updated: 2026-10-06
sources: [b04b41e, commandset/Models/Common/AIResult.cs, commandset/Utils/McpResolveUtils.cs, commandset/Models/Common/JZPoint.cs]
related: [../concepts/ai-grade-conventions.md, 0001-extend-via-four-part-pattern.md, 0017-elementid-extensions-over-preprocessor.md]
tags: [conventions, units, errors]
---

# 0005 AI-grade tool conventions

## Context

These tools are driven by an LLM that sends messy, partial input and reads the reply text to
correct itself. The first Cemengal roadmap (`TOOLSET_ROADMAP.md` in `b04b41e`, merged into
`CEM_RevitMCP.md` §4 by `aa88b96`) fixed the contract every new tool follows.

## Decision

1. **mm in, mm out** (angles in degrees). Convert only at the boundary, with `McpResolveUtils.MmToFt`
   / `FtToMm` and `JZPoint.ToXYZ`.
2. Return **`AIResult<T>`** (`Success`, `Message`, `Response`) with a human-readable `Message`,
   including a `⚠ Warnings` summary.
3. **Never hard-fail a batch.** Skip the bad item, warn, and continue. A batch of 50 must not die on
   item 7.
4. Accept **display names and enum names** (`BuiltInParameter`, `BuiltInCategory`), for example via
   `McpResolveUtils.TryResolveBuiltInCategory` and `ResolveCategory`.
5. **Idempotent by name** where creation is involved: reuse existing filters, types and params, and
   don't error on "already exists".
6. One Command + one EventHandler per tool. The handler owns its `Transaction`, on the **family
   doc** for family geometry ([0009](0009-small-composable-family-tools.md)).
7. **Multi-version safe** (Revit 2020–2026): use `REVIT2022_OR_GREATER` etc. and
   `ElementIdExtensions` instead of `new ElementId(int)` ([0017](0017-elementid-extensions-over-preprocessor.md)).
8. `verb_noun` snake_case, domain-prefixed for non-core tools ([0004](0004-prefix-equals-folder-equals-namespace.md)).
9. The TS zod schema mirrors the C# DTO field for field, and **every field has a description**,
   because the LLM reads them.

## Consequences

- The 23 upstream commands predate these rules and do not all follow them (several return their own
  result objects rather than `AIResult`). Apply the rules to new and touched Cemengal tools; don't
  rewrite upstream tools just for conformance.
- `CommandExecutor.DescribeResult` extracts `message`/`Message` from results for the audit log
  ([0013](0013-hardcoded-failsoft-audit-trails.md)), so a good `Message` also makes sessions
  reviewable.
- Detail and examples: [AI-grade conventions](../concepts/ai-grade-conventions.md).
