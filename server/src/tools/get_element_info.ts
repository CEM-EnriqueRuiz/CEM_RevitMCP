import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetElementInfoTool(server: McpServer) {
  server.tool(
    "get_element_info",
    "Compact identity/placement info for elements: name, category, family, type, level, host.",
    { elementIds: z.array(z.number().int()).min(1).describe("Element ids") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("get_element_info", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "get_element_info failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
