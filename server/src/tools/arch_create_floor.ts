import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

export function registerArchCreateFloorTool(server: McpServer) {
  server.tool(
    "arch_create_floor",
    "Create a floor from a closed boundary of points (mm). The loop auto-closes. Floor type and level are resolved by name (or defaulted).",
    {
      boundary: z.array(point).min(3).describe("Closed boundary points (mm); auto-closed"),
      typeName: z.string().optional().describe("Floor type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
      structural: z.boolean().default(false).describe("Mark as structural"),
    },
    async (args) => {
      const params = { boundary: args.boundary, typeName: args.typeName ?? "", level: args.level ?? "", structural: args.structural };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("arch_create_floor", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `arch_create_floor failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
