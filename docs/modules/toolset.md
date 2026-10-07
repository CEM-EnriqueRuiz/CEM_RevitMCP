---
title: The toolset (120 commands by domain)
type: module
updated: 2026-10-06
sources: [command.json, commandset/Commands, server/src/tools, 86cf705]
related: [../concepts/domains-and-naming.md, ../decisions/0006-create-methods-not-full-parity.md, ../concepts/upstream-and-fork.md]
tags: [toolset, catalog]
---

# The toolset (120 commands by domain)

`command.json` is the source of truth: 120 entries, all matched by a C# `CommandName`. **118 have a
TS tool.** `tag_walls` and `tag_rooms` are registered in C# but have no TS wrapper. This is **deliberate**: they were
consolidated into the single `tag_elements` tool, and the C# side still serves both names (user note in
`CEM_RevitMCP.md`, `29b3fba`). Regenerate with `grep -o '"commandName"[^,]*' command.json`.

Grouped by **C# folder** (what you open to edit). Items marked † are **upstream** (23); everything
else was added by Cemengal.

| Folder | n | Commands |
|---|---|---|
| `Access` | 8 | ai_element_filter†, get_available_family_types†, get_current_view_elements†, get_current_view_info†, get_selected_elements†, operate_element†, get_element_parameters, get_warnings |
| `AnnotationComponents` | 5 | create_dimensions†, tag_rooms† (no TS), tag_walls† (no TS), tag_elements, take_screenshot |
| `Architecture` | 14 | arch_create_wall, arch_create_floor, arch_create_ceiling, arch_create_roof, arch_create_curtain_wall, arch_create_opening, arch_create_room, arch_create_stairs, arch_create_railing, arch_create_area, arch_create_area_plan, arch_create_separator, arch_join_geometry, arch_unjoin_geometry |
| `Core`: geometry base | 7 | create_grid†, create_level†, create_room†, create_point_based_element†, create_line_based_element†, create_surface_based_element†, color_splash† |
| `Core`: params/edit | 17 | set_element_parameters, copy_parameter_values, bulk_set_by_filter, list_parameters_for_category, transform_elements, delete_elements, group_elements, ungroup_group, pin_elements, rename_element, duplicate_type, find_elements, get_element_info, get_element_geometry, create_shared_parameter, create_project_parameter, create_global_parameter |
| `Core`: DB-gap fills | 10 | create_reference_plane, create_model_lines, create_detail_lines, create_direct_shape, create_spot_dimension, create_assembly, create_multi_segment_grid, edit_curtain_grid, place_image, create_parts |
| `Core`: styles | 3 | manage_line_pattern, manage_fill_pattern, set_object_styles |
| `Core`: `coord_*` | 5 | coord_manage_worksets, coord_link_model, coord_purge_unused, coord_audit_model, coord_manage_phases |
| `Core`: `viz_*` | 4 | viz_create_material, viz_set_material_appearance, viz_list_materials, viz_assign_material |
| `DataExtraction` | 3 | analyze_model_statistics†, export_room_data†, get_material_quantities† |
| `Delete` | 1 | delete_element† |
| `ExecuteDynamicCode` | 1 | send_code_to_revit† ([module](send-code-to-revit.md)) |
| `Family` | 13 | family_load, family_place_instance, family_get_types, family_create_type, family_add_parameter, family_add_type, family_set_formula, family_associate_parameter, family_add_reference_plane, family_open_session, family_save_session, family_create_extrusion, family_create_revolution ([module](family-editing.md)) |
| `Mep` | 7 | mep_create_duct, mep_create_pipe, mep_create_conduit, mep_create_cable_tray, mep_place_equipment, mep_create_space, mep_get_system_info |
| `ParentTools` | 4 | parent_link_extract_geometry, parent_solid_to_member_params, parent_reconstruct_native, parent_validate_deviation ([module](parent-tools.md)) |
| `Struct` | 5 | create_structural_framing_system†, struct_create_beam, struct_create_brace, struct_create_column, struct_create_foundation |
| `Test` | 1 | say_hello† |
| `Views` | 12 | create_view, duplicate_view, set_view_properties, create_sheet, place_viewport, create_schedule, create_legend, create_view_filter, apply_filter_to_view, place_text, create_filled_region, create_revision |

## Overlaps to know about

- `create_room` (upstream, Core) and `arch_create_room` (Cemengal) both create rooms.
- `get_available_family_types` (upstream) and `family_get_types` (Cemengal) overlap.
- `delete_element` (upstream) and `delete_elements` (Cemengal batch).
- `color_splash`'s TS file is `color_splash.ts`; upstream used to call the TS tool
  `color_elements` ([0012](../decisions/0012-drop-sqlite-store-pack.md)).

## Build order history (all planned packs complete)

1. Core (params, transform, group/pin/rename, views/sheets/viewports, filters, tags, warnings,
   queries)
2. MEP
3. Family (project and editor)
4. Architecture
5. Core DB extensions (schedules, view props, filters-to-view, legend, text, filled region,
   revision, line/fill patterns, object styles)
6. `viz_*`
7. `coord_*`
8. `struct_*`
9. DB-gap fills (P1: model/detail lines, direct shape, spot dim, assembly, multi-segment grid; P2:
   curtain-grid edit, image, parts)
10. ParentTools
11. Family-edit session and geometry upgrade, 2026-06-14

Steps 1–4 landed in `b04b41e`; the rest in `aa88b96`. What's deferred and why:
[0006](../decisions/0006-create-methods-not-full-parity.md). Persona filtering:
[0007](../decisions/0007-personas-deferred.md).
