import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerUngroupGroupTool(server: McpServer) {
  server.tool(
    "ungroup_group",
    "Ungroup one or more model groups into their member elements.",
    { elementIds: z.array(z.number().int()).min(1).describe("Group element ids") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("ungroup_group", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "ungroup_group failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
