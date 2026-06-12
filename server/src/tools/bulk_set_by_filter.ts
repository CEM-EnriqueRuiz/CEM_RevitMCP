import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerBulkSetByFilterTool(server: McpServer) {
  server.tool(
    "bulk_set_by_filter",
    "Set a parameter on every element of the given categories (optionally only in the active view). Use parameterName or builtInParameter; length/angle values are mm/deg.",
    { categories: z.array(z.string()).min(1).describe("BuiltInCategory names"), parameterName: z.string().optional(), builtInParameter: z.string().optional(), value: z.union([z.string(),z.number(),z.boolean()]).describe("Value to set"), activeViewOnly: z.boolean().default(false), isTypeParameter: z.boolean().default(false) },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("bulk_set_by_filter", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "bulk_set_by_filter failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
