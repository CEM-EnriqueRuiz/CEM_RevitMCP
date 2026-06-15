import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerCreateSpotDimensionTool(server: McpServer) {
  server.tool(
    "create_spot_dimension",
    'Create a spot elevation (kind="elevation", default) or spot coordinate (kind="coordinate") on an element at a point (mm) in a view. A geometric reference is taken from the element. bend/end position the leader/text; omit for sensible offsets. View resolved by id/name (-1/empty = active).',
    {
      viewId: z.string().optional().describe("View id or name; omit/-1 for active view"),
      elementId: z.number().int().describe("Element to dimension"),
      point: point.describe("Point on the element (mm)"),
      bend: point.optional().describe("Leader bend point (mm); optional"),
      end: point.optional().describe("Text/end point (mm); optional"),
      kind: z.enum(["elevation", "coordinate"]).default("elevation").describe("Spot kind"),
      hasLeader: z.boolean().default(true).describe("Draw a leader"),
    },
    async (args) => {
      const params = { ...args, viewId: args.viewId ?? "", bend: args.bend ?? null, end: args.end ?? null };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_spot_dimension", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_spot_dimension failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
