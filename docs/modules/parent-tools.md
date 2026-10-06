---
title: ParentTools (parent_*) — IFC-link → native reconstruction pipeline
type: module
updated: 2026-10-06
sources: [aa88b96, commandset/Commands/ParentTools/ParentToolCommands.cs, commandset/Services/ParentTools, commandset/Models/Common/ParentToolModels.cs, server/src/tools/parent_link_extract_geometry.ts]
related: [../decisions/0008-parent-tools-from-recipe-corpus.md, ../concepts/audit-trails.md, toolset.md]
tags: [parent-tools, ifc, geometry]
---

# ParentTools (parent_*) — IFC-link → native reconstruction pipeline

**Origin: Cemengal** (`aa88b96`). Why it exists: [0008](../decisions/0008-parent-tools-from-recipe-corpus.md).

These are pipeline tools, not single-API wrappers. Together they replace the recipes the AI used to
hand-write through `send_code_to_revit`:

| Step | Tool | Mode | Does |
|---|---|---|---|
| 1 | `parent_link_extract_geometry` | read-only | inspect a loaded Revit/IFC link: element counts by category and, optionally, per-element geometry summaries (bbox and volume in mm, link transform applied) |
| 2a | `parent_solid_to_member_params` | read-only | derive linear-member parameters from linked elements: best-fit centerline start/end, section breadth×height, length, nearest level (mm). Feed these into `struct_create_beam`, `struct_create_column` or `arch_create_wall` for fully typed native elements |
| 2b | `parent_reconstruct_native` | write | rebuild one category of a link as native **DirectShape** copies of the source solids. Idempotent: each rebuild is tagged with a CEMAI app-id plus a back-pointer to its source, and `clearPrevious` removes the prior pass |
| 3 | `parent_validate_deviation` | read-only | for every CEMAI-tagged rebuild, sample source surfaces and measure distance to the nearest native face: per-item and overall avg/worst deviation (mm), count over tolerance |

There are two reconstruction routes. 2a produces real typed elements (walls, beams, columns), and
2b produces robust DirectShape copies for anything else.

Code: four Command classes in `commandset/Commands/ParentTools/ParentToolCommands.cs`, handlers in
`commandset/Services/ParentTools/`, DTOs in `Models/Common/ParentToolModels.cs`, and one TS file per
tool. For exact fields, read the TS zod schema of each tool; it mirrors the DTO.

Geometry operations are heavy, so use the long timeouts (60–300 s) and keep them below the TS
client's 120 s ([threading](../concepts/threading-and-external-events.md)).
