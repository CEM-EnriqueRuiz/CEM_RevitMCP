import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerStructCreateBraceTool(server: McpServer) {
  server.tool(
    "struct_create_brace",
    "Create a structural brace (line-based) from start to end (mm) on a level, using a Structural Framing family type (by name/id; first available if omitted). Braces are typically diagonal between levels.",
    {
      start: point.describe("Brace start (mm)"),
      end: point.describe("Brace end (mm)"),
      typeName: z.string().optional().describe("Structural Framing type name or id"),
      level: z.string().optional().describe("Level name or id; omit for lowest level"),
    },
    async (args) => {
      const params = { ...args, typeName: args.typeName ?? "", level: args.level ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("struct_create_brace", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `struct_create_brace failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
