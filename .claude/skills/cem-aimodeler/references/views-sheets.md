# references/views-sheets.md — Views, sheets, schedules, tags, dimensions, legends, graphics

Covers the flat documentation core (`create_view`, `create_sheet`, `place_viewport`, `create_schedule`,
`tag_elements`, `create_dimensions`, `create_spot_dimension`, `place_text`, `create_legend`,
`duplicate_view`, `set_view_properties`) and graphics overrides (`set_object_styles`, `color_splash`,
`create_view_filter`, `apply_filter_to_view`, `manage_fill_pattern`, `manage_line_pattern`,
`create_filled_region`, `create_detail_lines`).

## Views
- `create_view` — any common kind: FloorPlan, CeilingPlan, AreaPlan, Section, Elevation, 3D, Drafting,
  Detail. Plans need a level (by name); sections/elevations need a placement/direction.
- `duplicate_view` — duplicate (with/without detailing, or as dependent). Cheaper than rebuilding.
- `set_view_properties` — view template, scale, crop box on/off + active, detail level, discipline,
  visibility settings. **Applying a view template locks the parameters it controls** — you can't then set
  scale/VG on that view until you detach or the template releases it. Set template-controlled props via the
  template, not the view.
- View **template creation from scratch has no API** — apply or duplicate an existing one (api-gotchas).

## Sheets & viewports
- `create_sheet` — from a title block family (by name, or first available). Number/name per the project DNS
  standard (CMGL/AMRIZE-Holcim document numbering).
- `place_viewport` — place view(s) as viewports on a sheet. Position is the viewport **center** in sheet
  space; set the viewport type for the title/extent line behavior.
- Activate the sheet and **screenshot**: check titleblock fields, viewport placement, scale, that the view
  isn't clipped by its crop.

## Schedules
- `create_schedule` — schedule for a category with chosen fields, plus optional filter/sort/group. Field
  names must match the category's parameters (use `list_parameters_for_category` first).
- For a quantities takeoff, prefer a material takeoff schedule / `get_material_quantities`.

## Annotation
- `tag_elements` — tag an explicit list of element ids, **or** every element of a category in a view. Tag
  family must be loaded; "no tag loaded for category" is the usual failure.
- `create_dimensions` — dimensions between references in a view. Needs valid references (faces/edges/grids),
  not arbitrary points.
- `create_spot_dimension` — spot **elevation** (default) or spot **coordinate** at a point on a face.
- `place_text` — text note at a point in a view (mm). `create_detail_lines` — view-specific detail lines
  from a polyline. `create_filled_region` — filled region from a closed boundary (≥3 pts, auto-closed).

## Legends
- `create_legend` — **duplicates an existing legend** (Revit has no create-legend API). There must be at
  least one legend in the project to duplicate.

## Graphics & overrides
- `set_object_styles` — category projection/cut line weight (1–16), color, pattern.
- `create_view_filter` (a `ParameterFilterElement` with AND rules) → `apply_filter_to_view` (set
  visible/override). This is the clean way to color or hide by parameter.
- `color_splash` — quick color-by-parameter-value for a category in the current view.
- `manage_fill_pattern` / `manage_line_pattern` — list, or create simple patterns.

## Sequencing
1. Create/duplicate the view, set scale/detail/crop (or via template).
2. Apply view filters/overrides for the intended graphics.
3. Tag + dimension + annotate.
4. Create the sheet, place viewports, screenshot the sheet, fix placement/clipping.
5. Schedules last (after parameters are populated).

## Gotchas
- View template applied → can't set the props it controls; change the template instead.
- Tagging with no tag family loaded for the category → fails; load the tag first.
- Dimension to invalid references → fails; pick faces/edges/grids.
- Viewport position is center-based; off-sheet viewports look "missing" — screenshot to confirm.
- Legend with no existing legend to duplicate → can't be created.
