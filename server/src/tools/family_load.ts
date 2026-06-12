import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFamilyLoadTool(server: McpServer) {
  server.tool(
    "family_load",
    "Load a family (.rfa) from an absolute file path into the current project, overwriting an existing family (and its parameter values) by default. Returns the family id followed by its type ids.",
    {
      path: z.string().describe("Absolute path to the .rfa file"),
      overwrite: z.boolean().default(true).describe("Overwrite the family and parameter values if it already exists"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("family_load", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `family_load failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
