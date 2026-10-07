---
title: Domains and naming
type: concept
updated: 2026-10-07
sources: [commandset/Utils/RevitAPI_2024_namespaces.md, CEM_RevitMCP.md]
related: [../decisions/0003-domains-from-api-namespaces.md, ../decisions/0004-prefix-equals-folder-equals-namespace.md, ../modules/toolset.md]
tags: [domains, naming]
---

# Domains and naming

Domains come from Revit API namespaces ([0003](../decisions/0003-domains-from-api-namespaces.md)).
The tool prefix, the folder and the namespace suffix are the same string
([0004](../decisions/0004-prefix-equals-folder-equals-namespace.md)).

| Namespace | What lives there | Pack / prefix | Folder |
|---|---|---|---|
| `Autodesk.Revit.DB` (1078 classes, 151 create methods) | Wall, Floor, Grid, Level, ReferencePlane, FamilyInstance, views, sheets, schedules, Viewport, Dimension, TextNote, IndependentTag, FilledRegion, Group, Assembly, Phase, Material, CurtainGrid | **Core** (flat names) | `Core`, `Access`, `Views`, `AnnotationComponents`, `DataExtraction`, `Delete` |
| `DB.Architecture` (+ DB hosts) | Room, Stairs, Railing, BuildingPad, walls, floors, ceilings, roofs, openings | `arch_*` | `Architecture` |
| `DB.Mechanical` + `DB.Plumbing` + `DB.Electrical` | ducts, pipes, conduit, cable tray, systems, equipment, spaces | `mep_*` | `Mep` |
| DB family API (`Family`, `FamilySymbol`, `FamilyManager`, `FamilyItemFactory`) | load and place families, editor geometry and params | `family_*` | `Family` |
| `DB.Structure` | framing, columns, footings (no rebar/loads/analytical) | `struct_*` | `Struct` |
| worksharing / links / QA (+ `DB.IFC`) | worksets, links, phases, purge, audit | `coord_*` | `Core` |
| `DB.Visual` | material appearance | `viz_*` | `Core` |
| (pipelines, not one namespace) | IFC-link → native reconstruction | `parent_*` | `ParentTools` |
| (the Cemengal add-ins, sibling `CEM_RevitAPI`) | CEM Swap, CEM Rules, the Cemengal ribbon buttons | `cem_*` | `Cemengal` |

Also: `ExecuteDynamicCode` (`send_code_to_revit`) and `Test` (`say_hello`).

**Key truth:** views, sheets, schedules, tags, dimensions, text, filled regions, line and fill
patterns, and object styles all live in `Autodesk.Revit.DB`. They extend Core with flat names. Don't
create a "views" or "styles" domain.

Unused namespaces that are candidate future packs: `infra_*` (`DB.Infrastructure`), `fab_*`
(`DB.Fabrication`), `ifc_*`, `storage_*` (`DB.ExtensibleStorage`, explicitly excluded for now),
`pointcloud_*`, `analysis_*`.

## Name shapes

- Core: `verb_noun` (`set_element_parameters`, `create_view`, `tag_elements`).
- Domain: `domain_verb_noun` (`arch_create_wall`, `mep_create_duct`, `family_load`,
  `struct_create_beam`, `coord_link_model`, `viz_create_material`, `parent_validate_deviation`).
- A TS tool must use the exact `command.json` name (for example `color_splash`, not
  `color_elements`).
