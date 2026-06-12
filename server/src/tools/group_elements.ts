import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGroupElementsTool(server: McpServer) {
  server.tool(
    "group_elements",
    "Group two or more elements into a model group; optional name.",
    { elementIds: z.array(z.number().int()).min(2).describe("Elements to group"), name: z.string().optional().describe("Optional group type name") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("group_elements", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "group_elements failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
