import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilyAddTypeTool(server: McpServer) {
  server.tool(
    "family_add_type",
    "Add a new type to the family currently open in the Family Editor (an .rfa). Fails clearly if the active document is a project, not a family.",
    {
      typeName: z.string().describe("Name for the new family type"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_add_type", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_add_type failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
