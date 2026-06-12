import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerPinElementsTool(server: McpServer) {
  server.tool(
    "pin_elements",
    "Pin or unpin elements (lock their position).",
    { elementIds: z.array(z.number().int()).min(1).describe("Elements"), pinned: z.boolean().default(true).describe("true=pin, false=unpin") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("pin_elements", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "pin_elements failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
