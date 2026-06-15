import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateScheduleTool(server: McpServer) {
  server.tool(
    "create_schedule",
    "Create a schedule (planification table) for a category, with chosen columns and optional sort/group. Field names accept BuiltInParameter or display names; only schedulable fields are added (others are skipped with a warning). Returns the schedule id and the fields actually added.",
    {
      category: z.string().describe('BuiltInCategory to schedule, e.g. "OST_Walls", "OST_Doors", "OST_Rooms"'),
      name: z.string().optional().describe("Optional schedule name"),
      fields: z.array(z.string()).optional().describe("Column field names in order; omit for a sensible default set"),
      sortBy: z.string().optional().describe("Field name to sort/group by (must be one of fields)"),
      sortOrder: z.enum(["Ascending", "Descending"]).default("Ascending").describe("Sort order when sortBy is set"),
      itemized: z.boolean().default(true).describe("Itemize every instance (true) or roll up (false)"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_schedule", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_schedule failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
