import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerEditCurtainGridTool(server: McpServer) {
  server.tool(
    "edit_curtain_grid",
    "Add grid lines to a curtain wall or curtain system. uGridPoints/vGridPoints are points (mm) on the curtain surface where each new U or V grid line passes. Set oneSegmentOnly=true to add a single segment at the point instead of a full line. Per-point failures are reported, not fatal.",
    {
      hostId: z.number().int().describe("Curtain wall / curtain system element id"),
      uGridPoints: z.array(point).optional().describe("Points (mm) for new U grid lines"),
      vGridPoints: z.array(point).optional().describe("Points (mm) for new V grid lines"),
      oneSegmentOnly: z.boolean().default(false).describe("Add a single segment instead of a full grid line"),
    },
    async (args) => {
      const params = { ...args, uGridPoints: args.uGridPoints ?? [], vGridPoints: args.vGridPoints ?? [] };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("edit_curtain_grid", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `edit_curtain_grid failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
