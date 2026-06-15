import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerVizListMaterialsTool(server: McpServer) {
  server.tool(
    "viz_list_materials",
    "List materials in the document with id, name, shading color [r,g,b], transparency and material class. Use this for discovery before assigning or editing materials. Optional case-insensitive name substring filter.",
    {
      nameFilter: z.string().optional().describe("Case-insensitive substring to filter material names"),
    },
    async (args) => {
      const params = { nameFilter: args.nameFilter ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("viz_list_materials", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `viz_list_materials failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
