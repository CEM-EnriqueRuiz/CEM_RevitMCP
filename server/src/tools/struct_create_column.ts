import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const point = z.object({ x: z.number(), y: z.number(), z: z.number().default(0) });

export function registerStructCreateColumnTool(server: McpServer) {
  server.tool(
    "struct_create_column",
    "Create a structural column at a point (mm) on a base level, using a Structural Columns family type (by name/id; first available if omitted). Optionally set a top level by name/id so the column spans between levels.",
    {
      location: point.describe("Column location (mm)"),
      typeName: z.string().optional().describe("Structural Columns type name or id"),
      baseLevel: z.string().optional().describe("Base level name or id; omit for lowest level"),
      topLevel: z.string().optional().describe("Top level name or id (optional)"),
    },
    async (args) => {
      const params = { ...args, typeName: args.typeName ?? "", baseLevel: args.baseLevel ?? "", topLevel: args.topLevel ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("struct_create_column", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `struct_create_column failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
