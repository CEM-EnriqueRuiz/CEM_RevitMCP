# Smoke Test — Revit MCP (CEM_RevitAI), 54 tools

Goal: confirm the full chain works end-to-end before building more packs:

```
Claude (MCP client)  ──►  local TS server (build/index.js)  ──►  TCP :8080  ──►  RevitMCPPlugin  ──►  RevitMCPCommandSet (54 commands)
```

Everything is already **built and deployed** (Revit 2024 Addins folder, 54 commands). You only need
to wire the MCP client and run a few calls.

---

## 0. Prerequisites (one-time)

- Revit 2024 installed; a **project** document to test in (and optionally an **.rfa** for the family-editor tools).
- The local server is built: `server/build/index.js` exists. If not:
  ```powershell
  cd d:\repos\Cemengal\CEM_IA\CEM_RevitMCP\mcp-servers-for-revit\server
  npm install   # first time only
  npm run build
  ```

## 1. Point your MCP client at the LOCAL build (not the npm package)

The README's `npx -y mcp-server-for-revit` pulls the **published** server, which does NOT have your
new tools. Use your local build instead.

**Claude Desktop** → Settings → Developer → Edit Config → `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "cem-revit-ai": {
      "command": "node",
      "args": ["d:\\repos\\Cemengal\\CEM_IA\\CEM_RevitMCP\\mcp-servers-for-revit\\server\\build\\index.js"]
    }
  }
}
```

Restart Claude Desktop. When the tools/hammer icon shows `cem-revit-ai`, the server is connected.
(It will not reach Revit yet — that needs step 2.)

> Claude Code CLI alternative:
> `claude mcp add cem-revit-ai -- node "d:\repos\Cemengal\CEM_IA\CEM_RevitMCP\mcp-servers-for-revit\server\build\index.js"`

## 2. Start the Revit side

1. Open **Revit 2024** and open a project.
2. On the Cemengal ribbon tab, click **CEM_RevitAI**. A dialog should say **"Open Server"** — this
   starts the socket listener on port 8080 and loads the 54 background commands from
   `…\Addins\2024\CEM_RibbonUI\Commands\` (next to RevitMCPPlugin.dll).
3. Leave Revit open with that project active.

> Clicking the button again says "Close Server" and stops it. Toggle it back on to retest.

## 3. Run the test calls (in Claude, with the project open)

Do these **in order** — read-only first (safe), then creation. Each tool returns an `AIResult`
with a `Success` flag and a human-readable `Message`; check those.

### A. Read-only (verifies the chain, changes nothing)
1. **get_warnings** — "Get the model warnings."
   - Expect: a grouped summary (or "0 warnings"). Proves request → Revit → response works.
2. **family_get_types** — "List family types in category OST_Doors."
   - Expect: a list (or empty if no doors loaded). Proves filtering + reflection.
3. **get_element_parameters** — select one element in Revit, note its id, then
   "Read parameters of element <id>."
   - Expect: instance + type params with values.

### B. Creation (will modify the model — use a scratch project)
4. **create_view** — "Create a FloorPlan view named 'MCP Test Plan'."
   - Expect: `Success: true`, a new view id; find it in the Project Browser.
5. **arch_create_wall** — "Create a wall from (0,0,0) to (5000,0,0), height 3000."
   - Expect: a wall id; a 5 m wall appears on the lowest level.
6. **transform_elements** — "Copy element <that wall id> by (0,2000,0)."
   - Expect: a new wall id 2 m away.
7. **mep_create_pipe** — "Create a pipe from (0,0,0) to (3000,0,0), diameter 50."
   - Expect: a pipe id (needs a pipe type + piping system type to exist in the project).

### C. Family editor (optional — only if you open an .rfa)
8. Open a **family** (.rfa) so it's the active document, then
   **family_add_parameter** — "Add a Length type parameter 'MCP_Test' in group Dimensions."
   - Expect: `Success: true`. Verify in Family Types dialog.
   - In a **project** doc it should fail clearly with "only works inside the Family Editor".

## 4. What "pass" looks like

- Read-only tools return data without errors.
- Creation tools return `Success: true` + ids, and the elements actually appear in Revit.
- Family-editor tools succeed in an .rfa and fail with a clear message in a project.

## 5. If something fails — quick triage

| Symptom | Likely cause | Fix |
|---|---|---|
| Tool error "connect to revit client failed" | Server not started in Revit, or wrong port | Click **CEM_RevitAI** (must say "Open Server"); it's port 8080 |
| Claude doesn't list the new tools | Client pointing at npm package, or stale build | Use the local `build/index.js` config (step 1); `npm run build` |
| "Command not found" for a tool name | Registry not deployed / name mismatch | Confirm `…\Addins\2024\CEM_RibbonUI\Commands\commandRegistry.json` has 54 entries |
| Creation says "No <X>Type available" | Project lacks that type (e.g. no pipe/duct types) | Use a template/project that has MEP or load the type first |
| All MEP create calls fail | No MechanicalSystemType/PipingSystemType in doc | Test MEP in an MEP-enabled project template |
| Nothing happens but no error | Server loaded 0 commands | Rebuild CEM_RibbonUI **with Revit closed**, reopen, toggle server |

## 6. Report back

Tell me which of A/B/C passed and paste any `Message` from a failure. That tells me whether the
pattern is sound before I build the Structure and Coordination packs.

---

### Tool inventory (54) for reference
**Original (23):** say_hello, get_*, create_grid/level/room, create_point/line/surface_based_element,
color_splash, tag_walls, tag_rooms, create_dimensions, delete_element, ai_element_filter,
operate_element, send_code_to_revit, create_structural_framing_system, export_room_data,
get_material_quantities, analyze_model_statistics …
**Params/Views batch (5):** create_reference_plane, create_shared_parameter, set_element_parameters,
create_view_filter, create_global_parameter
**CORE (7):** get_element_parameters, transform_elements, create_view, create_sheet, place_viewport,
tag_elements, get_warnings
**MEP (7):** mep_create_duct, mep_create_pipe, mep_create_conduit, mep_create_cable_tray,
mep_place_equipment, mep_create_space, mep_get_system_info
**Architecture (5):** arch_create_wall, arch_create_floor, arch_create_ceiling, arch_create_room,
arch_create_opening
**Family (7):** family_load, family_place_instance, family_get_types, family_create_type,
family_add_parameter, family_add_type, family_set_formula
