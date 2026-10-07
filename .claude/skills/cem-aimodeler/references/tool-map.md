# references/tool-map.md — the MCP tools by prefix

The `cem-revit-ai` MCP server exposes ~126 tools that drive a **live Revit session**. Tools are named
`verb_noun` (flat core) or `domain_verb_noun`. Match the task to the prefix; only fall to
`send_code_to_revit` when no tool or composition of tools fits. Cemengal's own add-ins are reached through
the `cem_*` tools: see `cemengal-tools.md`.

All linear inputs are **millimetres (mm)**. Points are `[x, y, z]` in mm, project coordinates unless a tool
states otherwise. Types and levels are resolved **by name** wherever possible.

## Prefix groups

| Prefix | Domain | Reference |
|---|---|---|
| `cem_open_tool`, `cem_swap_find_types`, `cem_swap_list_presets`, `cem_swap_run`, `cem_swap_remove_trial`, `cem_rules_apply` | Cemengal add-ins (office tools) | cemengal-tools.md |
| `get_*`, `find_elements`, `ai_element_filter`, `analyze_model_statistics`, `list_parameters_for_category` | Read / inspect | — |
| `set_element_parameters`, `bulk_set_by_filter`, `copy_parameter_values`, `create_*_parameter`, `create_global_parameter` | Parameters | api-gotchas.md |
| `arch_*` | Architecture | architecture.md |
| `mep_*` | MEP | mep.md |
| `struct_*`, `create_structural_framing_system` | Structure | structure.md |
| `family_*` | Families (load/place/author) | families.md |
| `viz_*` | Materials & appearance | families.md (material section) |
| `create_view`, `create_sheet`, `create_schedule`, `place_viewport`, `tag_elements`, `create_dimensions`, `create_spot_dimension`, `place_text`, `create_legend`, `duplicate_view`, `set_view_properties`, `create_filled_region`, `create_detail_lines` | Views / sheets / annotation | views-sheets.md |
| `coord_*`, `parent_*`, `create_revision` | Coordination & IFC→native | coordination.md |
| `create_grid`, `create_multi_segment_grid`, `create_level`, `create_reference_plane`, `create_line_based_element`, `create_point_based_element`, `create_surface_based_element`, `create_model_lines`, `create_direct_shape`, `create_parts`, `create_assembly` | Datums & generic geometry | architecture.md / structure.md |
| `transform_elements`, `operate_element`, `group_elements`, `ungroup_group`, `pin_elements`, `rename_element`, `delete_element(s)`, `duplicate_type` | Edit / organise | api-gotchas.md |
| `take_screenshot`, `get_current_view_info`, `get_current_view_elements`, `get_selected_elements` | See / context | ../SKILL.md |
| `manage_fill_pattern`, `manage_line_pattern`, `set_object_styles`, `color_splash`, `apply_filter_to_view`, `create_view_filter` | Graphics / overrides | views-sheets.md |
| `export_room_data`, `get_material_quantities`, `get_element_geometry`, `get_element_info`, `get_element_parameters`, `get_warnings`, `coord_audit_model` | Extract / QA | coordination.md |
| `send_code_to_revit` | Last-resort C# | api-gotchas.md |

## Working rules
- **Office tool first.** If a Cemengal add-in does the job (replace elements, CEM Rules, filters…), use its `cem_*` tool.
- **List before you script.** If unsure a tool exists, enumerate tools and scan by prefix.
- **Compose, don't monolith.** `parent_solid_to_member_params` → `struct_create_beam` beats one giant snippet.
- **Read before write.** Use `get_*`/`find_elements` to resolve ids, levels and types before mutating.
- **Verify with eyes.** `take_screenshot` after each meaningful change (see the eyes loop in ../SKILL.md).
- **Idempotent + batch-safe.** Tools skip-and-report rather than hard-failing a batch; preserve that.
