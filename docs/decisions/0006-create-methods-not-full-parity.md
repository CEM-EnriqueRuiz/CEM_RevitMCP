---
title: "0006 Target create-methods plus common edits, not full API parity"
type: decision
status: current
updated: 2026-10-06
sources: [aa88b96, CEM_RevitMCP.md]
related: [../modules/toolset.md, 0007-personas-deferred.md, ../modules/send-code-to-revit.md]
tags: [scope, toolset]
---

# 0006 Target create-methods plus common edits, not full API parity

## Context

The Revit 2024 CHM lists 16,084 `Autodesk.Revit.DB` topics, but those are doc pages (every
overload, property, enum and event), not operations. The real build surface is about 151 DB create
methods plus the writable element families.

## Decision

The "senior-modeler complete" bar is **create methods + common edits**. Reads stay generic, through
`get_element_info`, `get_element_parameters`, `get_element_geometry` and `find_elements`.

**Rejected:** full property-level read/write parity. Past roughly 100 undifferentiated tools, LLM
tool selection degrades.

Deliberately deferred (documented, not forgotten):

- `ExtrusionRoof` (would change the `arch_create_roof` schema; add later as a `kind` flag),
  ModelText (family-doc only), wall/host sweeps (`WallSweepInfo`, fragile), BeamSystem and
  WallFoundation.
- The long tail goes to `send_code_to_revit`: conceptual mass (Form/Blend/Sweep/DividedSurface),
  adaptive components, site (Topography/BuildingPad/PropertyLine), AreaScheme, sun/shadow.
- **No public API:** scope boxes (only `VolumeOfInterest*` properties exist) and portable clash.
  Document these and defer to `send_code_to_revit`; don't ship a guess.
- Rebar, loads and analytical: fragile and version-sensitive (analytical was reworked in 2023+), so
  they go to `send_code_to_revit`.
- Also excluded: conceptual mass, site, model text, host sweeps, ExtensibleStorage.

## Consequences

- Before you add a tool, check existing tools. A per-namespace audit found Selection already
  covered (`operate_element` does `Selection.SetElementIds`), and Events correctly absent (they are
  host subscriptions, not tools).
- The tool count is already 120 (118 exposed to MCP). Growth should come from evidence (see
  [0008](0008-parent-tools-from-recipe-corpus.md)), not from coverage for its own sake.
