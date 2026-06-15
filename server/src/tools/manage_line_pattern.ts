import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerManageLinePatternTool(server: McpServer) {
  server.tool(
    "manage_line_pattern",
    "List line patterns, or create a new one. For create, give a name and alternating dash/gap segment lengths in mm (e.g. [5,2,1,2] = 5mm dash, 2mm gap, 1mm dash, 2mm gap). An existing pattern of the same name is reused (idempotent).",
    {
      action: z.enum(["list", "create"]).default("list").describe("List existing patterns or create one"),
      name: z.string().optional().describe("Pattern name (required for create)"),
      segmentsMm: z
        .array(z.number().positive())
        .optional()
        .describe("Alternating dash/gap lengths in mm (required for create, >=2 values)"),
    },
    async (args) => {
      const params = { ...args, name: args.name ?? "", segmentsMm: args.segmentsMm ?? [] };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("manage_line_pattern", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `manage_line_pattern failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
