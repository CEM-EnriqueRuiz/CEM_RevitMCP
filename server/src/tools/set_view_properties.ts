import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerSetViewPropertiesTool(server: McpServer) {
  server.tool(
    "set_view_properties",
    "Set common view properties: view template, scale, crop box visibility/active, detail level and discipline. View resolved by id or name (-1/empty = active). Any property controlled by an applied template is skipped and reported in warnings. Returns what was applied.",
    {
      viewId: z.string().optional().describe("View id or name; omit/-1 for active view"),
      templateName: z.string().optional().describe("View template name or id to apply"),
      scale: z.number().int().positive().optional().describe("Scale denominator, e.g. 100 for 1:100"),
      cropVisible: z.boolean().optional().describe("Show/hide the crop box"),
      cropActive: z.boolean().optional().describe("Activate/deactivate cropping"),
      detailLevel: z.enum(["Coarse", "Medium", "Fine"]).optional().describe("View detail level"),
      discipline: z
        .enum(["Architectural", "Structural", "Mechanical", "Electrical", "Plumbing", "Coordination"])
        .optional()
        .describe("View discipline"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("set_view_properties", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `set_view_properties failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
