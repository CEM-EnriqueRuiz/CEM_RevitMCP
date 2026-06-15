import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerManageFillPatternTool(server: McpServer) {
  server.tool(
    "manage_fill_pattern",
    "List fill (hatch) patterns, or create a simple parallel-line pattern. For create, give a name, angle (degrees), line spacing (mm) and target (Drafting or Model). An existing pattern of the same name is reused (idempotent).",
    {
      action: z.enum(["list", "create"]).default("list").describe("List existing patterns or create one"),
      name: z.string().optional().describe("Pattern name (required for create)"),
      angle: z.number().default(45).describe("Line angle in degrees (create)"),
      spacingMm: z.number().positive().default(5).describe("Spacing between lines in mm (create)"),
      target: z.enum(["Drafting", "Model"]).default("Drafting").describe("Pattern target (create)"),
    },
    async (args) => {
      const params = { ...args, name: args.name ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("manage_fill_pattern", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `manage_fill_pattern failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
