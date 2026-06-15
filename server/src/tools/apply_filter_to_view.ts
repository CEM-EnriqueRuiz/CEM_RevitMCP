import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerApplyFilterToViewTool(server: McpServer) {
  server.tool(
    "apply_filter_to_view",
    "Apply an EXISTING view filter (ParameterFilterElement) to a view: set whether matching elements are visible, plus optional graphic overrides (line/surface color, line weight, surface transparency). Create the filter first with create_view_filter. View resolved by id/name (-1/empty = active).",
    {
      viewId: z.string().optional().describe("View id or name; omit/-1 for active view"),
      filterName: z.string().describe("Existing filter name or id to apply"),
      visible: z.boolean().default(true).describe("Whether elements matching the filter are visible"),
      colorRgb: z.array(z.number().int().min(0).max(255)).length(3).optional().describe("Override color [r,g,b]"),
      lineWeight: z.number().int().min(1).max(16).optional().describe("Projection line weight 1-16"),
      transparency: z.number().int().min(0).max(100).optional().describe("Surface transparency 0-100"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("apply_filter_to_view", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `apply_filter_to_view failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
