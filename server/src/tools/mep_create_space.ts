import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

export function registerMepCreateSpaceTool(server: McpServer) {
  server.tool(
    "mep_create_space",
    "Create MEP spaces. Provide points (mm) to place a space at each, or omit points to auto-place spaces in all enclosed regions on the level. Optionally tag each created space.",
    {
      points: z.array(point).optional().describe("Points (mm) to place spaces; omit to auto-place in all enclosed regions"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
      tag: z.boolean().default(false).describe("Tag each created space"),
    },
    async (args) => {
      const params = { points: args.points ?? [], level: args.level ?? "", tag: args.tag };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("mep_create_space", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `mep_create_space failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
