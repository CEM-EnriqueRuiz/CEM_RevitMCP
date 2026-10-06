---
title: AI-grade conventions
type: concept
updated: 2026-10-06
sources: [commandset/Models/Common/AIResult.cs, commandset/Utils/McpResolveUtils.cs, commandset/Utils/McpParameterUtils.cs, commandset/Utils/McpFamilyUtils.cs, commandset/Models/Common/JZPoint.cs]
related: [../decisions/0005-ai-grade-tool-conventions.md, ../modules/commandset.md]
tags: [conventions]
---

# AI-grade conventions

The rules are in [0005](../decisions/0005-ai-grade-tool-conventions.md). This page covers how to
apply them with the helpers that already exist. **Extend `commandset/Utils/` rather than
re-implementing resolution in each handler.**

| Need | Use |
|---|---|
| mm ↔ ft | `McpResolveUtils.MmToFt` / `FtToMm`; `JZPoint.ToXYZ` (mm → ft) |
| element by id | `McpResolveUtils.GetElement(doc, long)`, `ToElementId(long)` |
| id → number | `id.GetValue()` / `GetIntValue()` ([0017](../decisions/0017-elementid-extensions-over-preprocessor.md)) |
| category by display or enum name | `McpResolveUtils.TryResolveBuiltInCategory`, `ResolveCategory` |
| level by name, id, or elevation | `McpResolveUtils.ResolveLevel(doc, nameOrId, elevationMm)` |
| type by name or id | `McpResolveUtils.ResolveType`, `FirstTypeOfClass` |
| curve loop from mm points | `McpResolveUtils.BuildCurveLoop` |
| view by name or id | `McpResolveUtils.ResolveView` |
| parameters | `McpParameterUtils` |
| family load / structural type | `McpFamilyUtils` (`OverwriteFamilyLoadOptions`, structural-type parse) |
| MEP helpers | `McpMepUtils` |
| result envelope | `AIResult<T> { Success, Message, Response }` |

## Message style

The LLM reads `Message` to self-correct. Write it as a summary, followed by a `⚠ Warnings` block
listing the skipped items and the reason for each. Example shape:
`"3 created, 1 skipped.\n⚠ Warnings:\n- item 2: no WallType 'Foo' (available: …)"`. Listing the
available alternatives when a name fails to resolve saves a round trip.

## Batches

Wrap each item in its own try/catch inside one transaction. Collect per-item results.
`Success = ok > 0 || fail == 0`. Never throw for a single bad item.

## Gotcha

`System.Drawing` collides with `Autodesk.Revit.DB` (both define `Rectangle` and `Color`), so alias
the GDI types. On net8 (R25/R26) `System.Drawing` needs the `System.Drawing.Common` package; on
net48 it is a framework reference (both are wired in the command-set csproj for `take_screenshot`).
