import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerCreateDirectShapeTool(server: McpServer) {
  server.tool(
    "create_direct_shape",
    'Create a DirectShape (free geometry element) in a category. shape="box" uses min/max corners (mm); shape="extrusion" extrudes a profile polygon (mm, >=3 pts) up by height (mm, +Z). Use for placeholder/imported masses or quick solids when no family fits.',
    {
      category: z.string().default("OST_GenericModel").describe('BuiltInCategory, e.g. "OST_GenericModel"'),
      shape: z.enum(["box", "extrusion"]).default("box").describe("Geometry kind"),
      min: point.optional().describe("Box: one corner (mm)"),
      max: point.optional().describe("Box: opposite corner (mm)"),
      profile: z.array(point).optional().describe("Extrusion: base profile vertices (mm, >=3)"),
      height: z.number().default(1000).describe("Extrusion: height in mm (+Z)"),
      name: z.string().optional().describe("Optional element name"),
    },
    async (args) => {
      const params = { ...args, min: args.min ?? null, max: args.max ?? null, profile: args.profile ?? [], name: args.name ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_direct_shape", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_direct_shape failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
