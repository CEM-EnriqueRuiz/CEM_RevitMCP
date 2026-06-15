import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreatePartsTool(server: McpServer) {
  server.tool(
    "create_parts",
    "Split elements (walls, floors, roofs, etc.) into Parts (PartUtils.CreateParts). Elements not valid for parts are skipped with a warning. Returns the created part ids — use these for further part division or per-layer scheduling.",
    {
      elementIds: z.array(z.number().int()).min(1).describe("Element ids to split into parts"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_parts", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_parts failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
