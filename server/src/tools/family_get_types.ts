import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilyGetTypesTool(server: McpServer) {
  server.tool(
    "family_get_types",
    "List loaded family types (FamilySymbols) with their family name, type name, category and id. Optionally filter by BuiltInCategory and/or family-name substring, and include each type's parameter names. Read-only — use it to discover what to place.",
    {
      category: z.string().optional().describe('BuiltInCategory filter, e.g. "OST_Doors"'),
      familyName: z.string().optional().describe("Family name substring filter"),
      includeParameters: z.boolean().default(false).describe("Include each type's parameter names"),
    },
    async (args) => {
      const params = { category: args.category ?? "", familyName: args.familyName ?? "", includeParameters: args.includeParameters };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_get_types", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_get_types failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
