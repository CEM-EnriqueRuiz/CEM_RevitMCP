import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateSheetTool(server: McpServer) {
  server.tool(
    "create_sheet",
    "Create a drawing sheet from a title block (resolved by name, or the first available). Optionally auto-place a list of views as viewports arranged in a grid. Returns the sheet id and any placed viewport ids.",
    {
      number: z.string().optional().describe('Sheet number, e.g. "A-101"'),
      name: z.string().optional().describe("Sheet name/title"),
      titleBlockName: z.string().optional().describe("Title block type name or id; omit for first available"),
      viewIds: z.array(z.number().int()).optional().describe("View ids to place on the sheet"),
    },
    async (args) => {
      const params = {
        number: args.number ?? "",
        name: args.name ?? "",
        titleBlockName: args.titleBlockName ?? "",
        viewIds: args.viewIds ?? [],
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_sheet", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_sheet failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
