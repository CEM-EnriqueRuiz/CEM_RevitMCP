import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

export function registerArchCreateRoomTool(server: McpServer) {
  server.tool(
    "arch_create_room",
    "Create rooms at points (mm) on a level, or omit points to auto-place rooms in all enclosed regions. Optional parallel names and tagging. Rooms require bounding elements (walls/separators) to enclose the point.",
    {
      points: z.array(point).optional().describe("Points (mm) to place rooms; omit to auto-place in all enclosed regions"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
      tag: z.boolean().default(false).describe("Tag each created room"),
      names: z.array(z.string()).optional().describe("Optional names parallel to points"),
    },
    async (args) => {
      const params = { points: args.points ?? [], level: args.level ?? "", tag: args.tag, names: args.names ?? [] };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("arch_create_room", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `arch_create_room failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
