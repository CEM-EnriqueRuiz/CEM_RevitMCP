import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerCreateDetailLinesTool(server: McpServer) {
  server.tool(
    "create_detail_lines",
    "Create view-specific detail lines from a polyline (points in mm) in a view. Each consecutive pair becomes one detail line. Optional line style (GraphicsStyle) by name. View resolved by id/name (-1/empty = active).",
    {
      viewId: z.string().optional().describe("View id or name; omit/-1 for active view"),
      points: z.array(point).min(2).describe("Polyline vertices (mm)"),
      closed: z.boolean().default(false).describe("Close the loop"),
      lineStyleName: z.string().optional().describe("Line style name to assign (optional)"),
    },
    async (args) => {
      const params = { ...args, viewId: args.viewId ?? "", lineStyleName: args.lineStyleName ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_detail_lines", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_detail_lines failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
