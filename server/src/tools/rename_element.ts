import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerRenameElementTool(server: McpServer) {
  server.tool(
    "rename_element",
    "Rename an element or type by id.",
    { elementId: z.number().int().describe("Element/type id"), newName: z.string().describe("New name") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("rename_element", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "rename_element failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
