import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";
const paramOverride = z.object({ name: z.string(), value: z.union([z.string(),z.number(),z.boolean()]) });
export function registerDuplicateTypeTool(server: McpServer) {
  server.tool("duplicate_type",
    "Duplicate any element type (resolved by name or id) to a new named type and apply parameter overrides. Length/angle values are mm/deg.",
    { sourceTypeName: z.string().describe("Source type name or id"), newTypeName: z.string().describe("New type name"), parameters: z.array(paramOverride).optional().describe("Parameter overrides") },
    async (args) => { const params = { sourceTypeName: args.sourceTypeName, newTypeName: args.newTypeName, parameters: args.parameters ?? [] };
      try { const r = await withRevitConnection((c) => c.sendCommand("duplicate_type", params)); return { content: [{ type: "text", text: JSON.stringify(r, null, 2) }] }; }
      catch (e) { return { content: [{ type: "text", text: "duplicate_type failed: " + (e instanceof Error ? e.message : String(e)) }] }; } });
}
