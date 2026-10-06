---
title: "0003 Tool domains are derived from Revit API namespaces"
type: decision
status: current
updated: 2026-10-06
sources: [b04b41e, aa88b96, commandset/Utils/RevitAPI_2024_namespaces.md, commandset/Utils/RevitAPI_2024_classes_by_namespace.txt]
related: [0004-prefix-equals-folder-equals-namespace.md, ../concepts/domains-and-naming.md]
tags: [domains, naming, design]
---

# 0003 Tool domains are derived from Revit API namespaces

## Context

The first Cemengal batch was planned in `SPECIALIST_ARCHITECTURE.md` (`b04b41e`, later merged into
`CEM_RevitMCP.md`). At that point it was tempting to invent "wishlist" domains such as views,
styles, sheets, schedules and tagging.

## Decision

Domains come from the empirical Revit 2024 API namespace inventory, extracted from
`commandset/Utils/2024.chm` into `commandset/Utils/RevitAPI_2024_namespaces.md` and
`RevitAPI_2024_classes_by_namespace.txt`. Each namespace is a candidate domain.

Views, sheets, `ViewSchedule`, `IndependentTag`, dimensions, `TextNote`, `FilledRegion` and
`GraphicsStyle` all live in `Autodesk.Revit.DB`. They therefore extend **Core** with flat names and
are not separate domains. The only real appearance namespace is `Autodesk.Revit.DB.Visual`, which
becomes `viz_*`.

**Rejected:** inventing domains from task vocabulary (a "views pack", a "styles pack").

## Consequences

- Before you add a domain, check which namespace the class lives in.
- `Core` is the catch-all. By convention it also holds the `coord_*`, `viz_*`, style and DB-gap
  tools (`commandset/Commands/Core/CoordCommands.cs`, `VizCommands.cs`, `StyleCommands.cs`,
  `DbGapCommands.cs`, `DbGap2Commands.cs`).
- The CHM is a dev reference only and is never shipped. If it is ever added as `Content`, exclude
  it from CEM_RibbonUI's `Commands\` staging glob.
