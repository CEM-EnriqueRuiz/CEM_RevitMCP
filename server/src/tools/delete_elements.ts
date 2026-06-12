import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerDeleteElementsTool(server: McpServer) {
  server.tool(
    "delete_elements",
    "Delete elements by id (and their dependents). Returns the count removed.",
    { elementIds: z.array(z.number().int()).min(1).describe("Element ids to delete") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("delete_elements", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "delete_elements failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
