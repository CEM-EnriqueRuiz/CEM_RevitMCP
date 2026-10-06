---
title: "0010 No NewBlend/NewSweep tools; decompose into sloped extrusions"
type: decision
status: current
updated: 2026-10-06
sources: [aa88b96, CEM_RevitMCP.md]
related: [0009-small-composable-family-tools.md, ../modules/family-editing.md]
tags: [family, geometry, rejected-approach]
---

# 0010 No NewBlend/NewSweep tools; decompose into sloped extrusions

## Context

The original family plan (`b04b41e`) listed `create_family_geometry` over `FamilyItemFactory`
(`NewExtrusion`, `NewBlend`, `NewRevolution`, `NewSweep`).

## Decision

`NewBlend` and `NewSweep` are **excluded**. The recipe corpus showed that blend fights the API,
because it needs its profiles on different planes. Decompose the shape into sloped extrusions
instead: `family_create_extrusion` accepts an arbitrary `{normal, origin}` plane for this. Only
`family_create_extrusion` and `family_create_revolution` exist.

## Consequences

Do not add a blend or sweep tool without new evidence. If a shape truly needs one, use
`send_code_to_revit`.
