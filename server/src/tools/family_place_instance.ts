import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

export function registerFamilyPlaceInstanceTool(server: McpServer) {
  server.tool(
    "family_place_instance",
    "Place instances of a loaded family type (resolved by name) at one or more points (mm). Level-based by default, or hosted when hostId is given. Optional Z rotation (degrees) and structural type.",
    {
      typeName: z.string().describe('Family type name or id, e.g. "Family : Type"'),
      points: z.array(point).min(1).describe("Placement points (mm)"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
      hostId: z.number().int().default(0).describe("Host element id (wall/face); 0 = level-based"),
      rotation: z.number().default(0).describe("Rotation about Z in degrees"),
      structuralType: z
        .enum(["NonStructural", "Beam", "Column", "Brace", "Footing"])
        .default("NonStructural")
        .describe("Structural role of the instance"),
    },
    async (args) => {
      const params = { typeName: args.typeName, points: args.points, level: args.level ?? "", hostId: args.hostId, rotation: args.rotation, structuralType: args.structuralType };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_place_instance", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_place_instance failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
