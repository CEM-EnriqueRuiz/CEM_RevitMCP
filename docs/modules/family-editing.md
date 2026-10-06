---
title: Family tools (family_*) and the family-edit session
type: module
updated: 2026-10-06
sources: [commandset/Commands/Family, commandset/Services/Family/FamilySessionEventHandlers.cs, commandset/Services/Family/FamilyGeomEventHandlers.cs, commandset/Models/Family/FamilyGeomModels.cs]
related: [../decisions/0009-small-composable-family-tools.md, ../decisions/0010-no-newblend-use-sloped-extrusions.md, toolset.md]
tags: [family]
---

# Family tools (family_*) and the family-edit session

**Origin: Cemengal** (`b04b41e`: project-context and editor params; `aa88b96`: session, geometry,
reference planes, associate).

There are two contexts. Guard every editor tool with `doc.IsFamilyDocument`.

| Context | Tools |
|---|---|
| Project (loaded families) | `family_load`, `family_place_instance`, `family_get_types`, `family_create_type` |
| Family editor (.rfa) | `family_add_parameter`, `family_add_type`, `family_set_formula`, `family_associate_parameter`, `family_add_reference_plane`, `family_create_extrusion`, `family_create_revolution` |
| Session | `family_open_session`, `family_save_session` |

## The session flow

1. `family_open_session`: open the family for editing. With `familyName`, it calls `EditFamily` on
   a family loaded in the active project. With `familyPath`, it reuses an already-open doc of that
   title or calls `OpenDocumentFile` (the file must exist). With neither, the active document must
   already be a family. `FamilySessionHelpers` finds open family docs by title.
2. The editor tools act on the open or active family document.
3. `family_save_session`: save, load back into the project (`OverwriteFamilyLoadOptions`), and/or
   close. If no family doc is open, it fails with "Open one with family_open_session first."

## family_create_extrusion

Multi-loop profiles (an outer loop plus holes), line and arc segments, arbitrary `{normal, origin}`
sketch planes (sloped extrusions replace blends, see
[0010](../decisions/0010-no-newblend-use-sloped-extrusions.md)), solid or void, and a material.
DTOs are in `Models/Family/FamilyGeomModels.cs`.

## Rules

- Transactions go on the **family document**, never the project.
- In a project document, editor tools must fail clearly. This is part of the
  [smoke test](../workflows/smoke-test.md).
- No `NewBlend`/`NewSweep` tool ([0010](../decisions/0010-no-newblend-use-sloped-extrusions.md)).
