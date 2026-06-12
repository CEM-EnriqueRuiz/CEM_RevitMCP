import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerArchUnjoinGeometryTool(server: McpServer) {
  server.tool(
    "arch_unjoin_geometry",
    "Unjoin geometry between two previously-joined elements.",
    { firstId: z.number().int(), secondId: z.number().int() },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("arch_unjoin_geometry", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "arch_unjoin_geometry failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
