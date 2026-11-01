---
title: Family tools (family_*) and the family-edit session
type: module
updated: 2026-11-01
sources: [https://github.com/CEMENGAL/CEM_Workspace/issues/5, commandset/Commands/Family, server/tests/guards/modeller-pins.test.ts, commandset/Services/Family/FamilySessionEventHandlers.cs, commandset/Services/Family/FamilyGeomEventHandlers.cs, commandset/Models/Family/FamilyGeomModels.cs]
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

1. `family_open_session`: open the family for editing and make it the **active** document.
   - With `familyName`, it calls `EditFamily` on a family loaded in the **host** (`FamilySessionHelpers.ResolveHost`):
     - `hostDocument`, when given: any open document by title, for example a container family, to edit a nested one;
     - otherwise the active document, if it is a project;
     - otherwise the open project (not a family, not a link);
     - with no project open, the active document.

     The active project comes first because, with two projects open, the first one in `Application.Documents` could be
     the wrong one, and a family of the same name would be edited and reloaded there (QA-7, 2026-11-01).
     It is never the active document by default, because after an earlier open that is a family, and its nested
     families would be searched instead. The modeller opens several families in a row, so this matters (QA-2,
     2026-11-01). The result names the host (`hostTitle`).
   - With `familyPath`, it reuses an open document only when it is **that file** (`FindOpenFamilyByPath`, full path),
     not one that merely shares the name. Otherwise it opens and activates the file (`OpenAndActivateDocument`; the
     file must exist).
   - With neither, the active document must already be a family.
   - `FamilySessionHelpers.Activate` then activates the family. Revit activates a document only from its file, so a
     family with no file (the `EditFamily` case) is first saved to a new
     `%TEMP%\Cemengal\RevitMCP\Families\<name>_<stamp>\<name>.rfa`, and activated there. The result warns and names
     the copy.
     - The copy is never overwritten (QA-5): one folder per copy, and the file keeps the family's name.
     - `family_save_session` saves a pathless family, or a family on ACC, the same way.
     - **A junction or link planted at the copy folder or file is refused (SEC-8).** The open then fails with a clear
       error: nothing is saved, and the unsaved family stays open
       ([audit trails](../concepts/audit-trails.md)).
   - `FamilySessionHelpers` finds open family docs by title.
2. The editor tools act on the **active** document only (`ActiveUIDocument.Document`). Until 2026-11-01,
   `family_open_session` never activated the family, so after a `familyName`/`familyPath` open the next `family_*`
   call hit the project and failed with "only works inside the Family Editor", although the open's message said
   otherwise.
3. `family_save_session`: save, load back into the project (`OverwriteFamilyLoadOptions`), and/or
   close. If no family doc is open, it fails with "Open one with family_open_session first."
   - Revit cannot close the active document, so `close` first re-activates the project (`OpenAndActivateDocument`
     of its path).
   - An unsaved project cannot be re-activated that way. Then the family stays open, with a warning.

Verified live on 2026-11-01 in Revit 2024:
- both open branches activate the family, and the warning branch of the close works;
- after the review fixes, `hostDocument`, the clear failure without it, the per-copy folders and the reuse by path
  all work.

The default host (the open project) is not checked live. The project re-activation is not checked live yet
([smoke test](../workflows/smoke-test.md#live-check-2026-11-01-revit-2024-after-a-cem_ribbonui-debug-r24-deploy)).

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
