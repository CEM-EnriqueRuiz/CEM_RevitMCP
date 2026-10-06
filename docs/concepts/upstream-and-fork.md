---
title: Upstream and fork boundary
type: concept
updated: 2026-10-06
sources: [d8c3223, 86cf705, b04b41e, aa88b96, README.md, .github/workflows/release.yml, scripts/release.ps1]
related: [../decisions/0001-extend-via-four-part-pattern.md, ../sources/commits/2026-02.md, ../sources/commits/2026-04.md, ../sources/commits/2026-06.md]
tags: [fork, upstream]
---

# Upstream and fork boundary

Remotes: `origin` = `CEM-EnriqueRuiz/CEM_RevitMCP`, `upstream` =
`mcp-servers-for-revit/mcp-servers-for-revit` (itself a fork of `revit-mcp`). The last upstream
commit in our history is `86cf705` (2026-04-05). Everything after it is Cemengal's. Root rule
([CROSS_REPO.md §5](../../../../CROSS_REPO.md)): extend via the 4-part pattern, and don't treat
upstream code as Cemengal-original.

## What is upstream (26 commits, 2026-02-07 → 2026-04-05)

- The architecture: TS stdio server → TCP socket → plugin → command set
  ([threading](threading-and-external-events.md)).
- `plugin/` core: `SocketService`, `CommandManager`, `CommandExecutor` (minus the audit hook),
  `ExternalEventManager`, configuration, the settings UI, `PathManager`, `Logger`.
- The `RevitMCPSDK` NuGet contracts (`ExternalEventCommandBase`, `IRevitCommand`,
  `IWaitableExternalEventHandler`, JSON-RPC models).
- **23 commands:** `ai_element_filter, analyze_model_statistics, color_splash, create_dimensions,
  create_grid, create_level, create_line_based_element, create_point_based_element, create_room,
  create_structural_framing_system, create_surface_based_element, delete_element,
  export_room_data, get_available_family_types, get_current_view_elements, get_current_view_info,
  get_material_quantities, get_selected_elements, operate_element, say_hello, send_code_to_revit,
  tag_rooms, tag_walls`.
- `tests/commandset/` TUnit integration suite (`ea54485`), `ElementIdExtensions` (`cd63a1e`),
  `send_code_to_revit` transaction mode (`17d0108`), `.github/workflows/release.yml`,
  `scripts/release.ps1`, `README.md` (still upstream's text), and `LICENSE` (MIT).

## What Cemengal added or changed (6 commits, 2026-06-13 →)

- **97 new commands** in the Core, Access, Views, Annotation, `arch_*`, `mep_*`, `family_*`,
  `struct_*`, `coord_*`, `viz_*` and `parent_*` packs ([toolset](../modules/toolset.md)).
- Renamed the projects: `RevitMCPPlugin` → `CEM_IAModeler`, `RevitMCPCommandSet` →
  `CEM_IAModeler_CommandSet`, `mcp-servers-for-revit.sln` → `CEM_RevitMCP.sln`. The namespaces
  followed.
- Domain folder alignment ([0004](../decisions/0004-prefix-equals-folder-equals-namespace.md)).
- Hosting by CEM_RibbonUI and the license gate ([0011](../decisions/0011-cem-ribbonui-hosts-deployment.md),
  [0014](../decisions/0014-license-gated-listener.md)).
- Audit trails ([0013](../decisions/0013-hardcoded-failsoft-audit-trails.md)) and
  `take_screenshot`.
- Removed the SQLite pack and dead TS tools ([0012](../decisions/0012-drop-sqlite-store-pack.md));
  added the single-file bundle ([0015](../decisions/0015-single-file-bundle.md)).
- The vitest schema suite ([0016](../decisions/0016-schema-only-ts-unit-tests.md)), `CLAUDE.md`,
  the skill, and this wiki.

## Divergence discipline

- The renames make upstream merges painful: every C# file's namespace differs. Pulling upstream
  means a manual port, not a merge. No upstream sync has happened since `86cf705`.
- `server/package.json` still carries upstream's npm identity (`mcp-server-for-revit`,
  repository/bugs/homepage URLs). Don't `npm publish` from here.
- `.github/workflows/release.yml` and `scripts/release.ps1` are upstream's release machinery and
  reference `mcp-servers-for-revit.sln`, which no longer exists. They are broken for this fork
  ([release](../workflows/release.md)).
