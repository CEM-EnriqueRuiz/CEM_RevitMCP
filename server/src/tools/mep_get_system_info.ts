import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerMepGetSystemInfoTool(server: McpServer) {
  server.tool(
    "mep_get_system_info",
    "Read the MEP system that an element belongs to (or that the id itself is): system name, type, member element ids, and flow. Read-only — use it to review duct/pipe/electrical system topology before editing.",
    {
      elementId: z.number().int().describe("An element id in the system, or a system element id"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("mep_get_system_info", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `mep_get_system_info failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
