import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateLegendTool(server: McpServer) {
  server.tool(
    "create_legend",
    "Create a legend view by duplicating an existing legend (Revit has no API to create a legend from scratch — at least one legend must already exist in the project). Optionally place it on a sheet by id. Returns the new legend view id and any viewport id.",
    {
      name: z.string().optional().describe("New legend view name"),
      scale: z.number().int().positive().optional().describe("Scale denominator, e.g. 50 for 1:50"),
      sheetId: z.number().int().optional().describe("Sheet id to place the legend on (optional)"),
    },
    async (args) => {
      const params = { ...args, sheetId: args.sheetId ?? 0 };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_legend", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_legend failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
