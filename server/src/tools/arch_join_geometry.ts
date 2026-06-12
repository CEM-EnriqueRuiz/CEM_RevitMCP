import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerArchJoinGeometryTool(server: McpServer) {
  server.tool(
    "arch_join_geometry",
    "Join geometry between two elements (e.g. wall and floor).",
    { firstId: z.number().int(), secondId: z.number().int() },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("arch_join_geometry", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "arch_join_geometry failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
