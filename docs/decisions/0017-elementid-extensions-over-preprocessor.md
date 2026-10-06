---
title: "0017 ElementIdExtensions instead of per-version preprocessor blocks"
type: decision
status: current
updated: 2026-10-06
sources: [cd63a1e, commandset/Utils/ElementIdExtensions.cs]
related: [0005-ai-grade-tool-conventions.md]
tags: [multi-version, upstream]
---

# 0017 ElementIdExtensions instead of per-version preprocessor blocks

**Origin: upstream** (`cd63a1e`, 2026-02-12).

## Context

`ElementId` became `long`-based in Revit 2024 (`.Value`), while older versions use `.IntegerValue`.
Handlers were full of `#if REVIT2024_OR_GREATER` blocks for every id access (about 130 duplicated
lines in `TagRoomsEventHandler.cs` alone).

## Decision

Use `id.GetValue()` (returns `long`) and `id.GetIntValue()` from
`commandset/Utils/ElementIdExtensions.cs`. Those are the only places with the `#if`. Never write
`new ElementId(int)` or branch on version for id access; to build an id, use
`McpResolveUtils.ToElementId(long)`.

## Consequences

The command set builds for Revit 2020–2026 from one source tree. `REVIT2022_OR_GREATER`,
`REVIT2023_OR_GREATER` and `REVIT2024_OR_GREATER` are still defined in the csproj for genuine API
differences. R25 and R26 target `net8.0-windows`; R20–R24 target `net48`.
