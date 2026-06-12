import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number() }).describe("Point in mm");

export function registerArchCreateCeilingTool(server: McpServer) {
  server.tool(
    "arch_create_ceiling",
    "Create a ceiling from a closed boundary of points (mm). The loop auto-closes. Ceiling type and level are resolved by name (or defaulted). Requires Revit 2022 or newer.",
    {
      boundary: z.array(point).min(3).describe("Closed boundary points (mm); auto-closed"),
      typeName: z.string().optional().describe("Ceiling type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
    },
    async (args) => {
      const params = { boundary: args.boundary, typeName: args.typeName ?? "", level: args.level ?? "", structural: false };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("arch_create_ceiling", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `arch_create_ceiling failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
