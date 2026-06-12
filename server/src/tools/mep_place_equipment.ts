import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

export function registerMepPlaceEquipmentTool(server: McpServer) {
  server.tool(
    "mep_place_equipment",
    "Place a loadable family (MEP equipment, fixture, terminal or device — resolved by type name) at one or more points (mm) on a level, with an optional Z-axis rotation in degrees. Works for any family type, not just MEP.",
    {
      typeName: z.string().describe('Family type name or id, e.g. "VAV Box" or "Family : Type"'),
      points: z.array(point).min(1).describe("Placement points (mm)"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
      rotation: z.number().default(0).describe("Rotation about Z in degrees"),
    },
    async (args) => {
      const params = { typeName: args.typeName, points: args.points, level: args.level ?? "", rotation: args.rotation };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("mep_place_equipment", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `mep_place_equipment failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
