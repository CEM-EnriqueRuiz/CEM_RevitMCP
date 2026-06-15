import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerDuplicateViewTool(server: McpServer) {
  server.tool(
    "duplicate_view",
    "Duplicate a view. Option: Duplicate (geometry only), WithDetailing (keeps annotations), or AsDependent. Source resolved by id or name (-1/empty = active view). Falls back to plain Duplicate with a warning if the chosen option is not allowed.",
    {
      viewId: z.string().optional().describe("Source view id or name; omit/-1 for active view"),
      option: z.enum(["Duplicate", "WithDetailing", "AsDependent"]).default("Duplicate").describe("Duplication mode"),
      newName: z.string().optional().describe("Optional new name (ignored for dependents)"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("duplicate_view", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `duplicate_view failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
