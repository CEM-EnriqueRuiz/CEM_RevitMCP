import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerStructCreateFoundationTool(server: McpServer) {
  server.tool(
    "struct_create_foundation",
    "Create an isolated structural foundation (footing) at a point (mm) on a level, using a Structural Foundation family type (by name/id; first available if omitted). For footing under a column, place at the column location.",
    {
      location: point.describe("Footing location (mm)"),
      typeName: z.string().optional().describe("Structural Foundation type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
    },
    async (args) => {
      const params = { ...args, typeName: args.typeName ?? "", level: args.level ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("struct_create_foundation", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `struct_create_foundation failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
