import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCemSwapListPresetsTool(server: McpServer) {
  server.tool(
    "cem_swap_list_presets",
    "CEMENGAL TOOL (CEM Swap, read-only) — the office's saved swaps in SwapPresets.json (the shared ACC file the CEM Swap window reads and writes): name, types to replace, new type, parameter map, offset, placement, conditions and watches. Prefer running a preset with cem_swap_run: it is what the users already validated.",
    {},
    async () => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("cem_swap_list_presets", {}));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `cem_swap_list_presets failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
