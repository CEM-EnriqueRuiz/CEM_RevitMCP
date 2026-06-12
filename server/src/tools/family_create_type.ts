import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

const paramOverride = z.object({
  name: z.string().describe("Parameter name"),
  value: z.union([z.string(), z.number(), z.boolean()]).describe("Value; mm/deg for length/angle params"),
});

export function registerFamilyCreateTypeTool(server: McpServer) {
  server.tool(
    "family_create_type",
    "Duplicate a loaded family type to a new named type and apply parameter overrides. Project context (works on loaded families, not the family editor). Length/angle values are mm/deg.",
    {
      sourceTypeName: z.string().describe("Source type name or id to duplicate"),
      newTypeName: z.string().describe("Name for the new type"),
      parameters: z.array(paramOverride).optional().describe("Parameter overrides to apply to the new type"),
    },
    async (args) => {
      const params = { sourceTypeName: args.sourceTypeName, newTypeName: args.newTypeName, parameters: args.parameters ?? [] };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_create_type", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_create_type failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
