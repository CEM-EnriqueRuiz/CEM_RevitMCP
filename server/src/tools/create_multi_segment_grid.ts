import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerCreateMultiSegmentGridTool(server: McpServer) {
  server.tool(
    "create_multi_segment_grid",
    "Create a multi-segment grid from a polyline chain (points in mm, >=2). Unlike create_grid (single straight grid), this makes one grid that follows multiple connected segments. Optional grid type by name/id.",
    {
      points: z.array(point).min(2).describe("Grid chain vertices (mm)"),
      gridTypeName: z.string().optional().describe("Grid type name or id (optional)"),
    },
    async (args) => {
      const params = { ...args, gridTypeName: args.gridTypeName ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_multi_segment_grid", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_multi_segment_grid failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
