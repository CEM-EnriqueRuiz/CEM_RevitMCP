import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCoordPurgeUnusedTool(server: McpServer) {
  server.tool(
    "coord_purge_unused",
    "Purge unused elements (unused types, patterns, materials, etc.). Requires Revit 2024+. Optionally limit to BuiltInCategory names. Set dryRun=true to report candidates (with a sample) without deleting. Returns candidate and deleted counts.",
    {
      categories: z.array(z.string()).optional().describe('Limit to categories, e.g. ["OST_Walls"]; omit for all'),
      dryRun: z.boolean().default(false).describe("Report only, do not delete"),
    },
    async (args) => {
      const params = { categories: args.categories ?? [], dryRun: args.dryRun ?? false };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("coord_purge_unused", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `coord_purge_unused failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
