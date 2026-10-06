---
title: "0009 Small composable family-editing tools, not a monolith"
type: decision
status: current
updated: 2026-10-06
sources: [aa88b96, commandset/Commands/Family/FamilySessionCommands.cs, commandset/Services/Family/FamilySessionEventHandlers.cs, commandset/Services/Family/FamilyGeomEventHandlers.cs]
related: [../modules/family-editing.md, 0010-no-newblend-use-sloped-extrusions.md]
tags: [family, design]
---

# 0009 Small composable family-editing tools, not a monolith

## Context

On 2026-06-14, 134 of that day's 198 recipes went to `send_code_to_revit` because family geometry
could not be built with the existing tools. A first approach, a monolithic
`parent_family_build_geometry` tool, was tried and abandoned, along with its orphan model file. It
was never committed: the name appears in git history only in the skill text (`b05a76f`), so the
evidence for it is that skill's account.

## Decision

Use small composable tools instead:

- `family_open_session`: open a family for editing (EditFamily, OpenDocumentFile, or the active
  doc).
- `family_create_extrusion`: upgraded to support multi-loop holes, line and arc segments, arbitrary
  `{normal, origin}` planes, and a material.
- `family_save_session`: save, load back into the project, and/or close.

Family geometry transactions must run on the **family document**, never the project document.

**Rejected:** a monolithic "build the whole family geometry" tool.

## Consequences

- Editor-only `family_*` tools act on the open or active family document. In a project document
  they should fail clearly (this is part of the smoke test).
- Shipped in `aa88b96`. Build-order step 11 in `CEM_RevitMCP.md` records that this closed the
  134/198 gap.
- Module page: [family editing](../modules/family-editing.md).
