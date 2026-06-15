import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({
  x: z.number().describe("X in mm"),
  y: z.number().describe("Y in mm"),
  z: z.number().describe("Z in mm").default(0),
});

export function registerCreateFilledRegionTool(server: McpServer) {
  server.tool(
    "create_filled_region",
    "Create a filled region from a boundary polygon (mm, auto-closed, >=3 points) in a detail-capable view (drafting view, plan, etc.). Optional FilledRegionType by name/id. View resolved by id/name (-1/empty = active). Returns the filled region id.",
    {
      viewId: z.string().optional().describe("View id or name; omit/-1 for active view"),
      boundary: z.array(point).min(3).describe("Boundary points (mm), closed automatically"),
      filledRegionTypeName: z.string().optional().describe("FilledRegionType name or id; omit for first available"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_filled_region", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_filled_region failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
