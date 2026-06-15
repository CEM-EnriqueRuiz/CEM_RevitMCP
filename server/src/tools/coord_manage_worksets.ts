import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCoordManageWorksetsTool(server: McpServer) {
  server.tool(
    "coord_manage_worksets",
    "Manage worksets in a WORKSHARED model (fails clearly otherwise). action=list returns user worksets; create makes one (idempotent by name); set_active sets the active workset; assign moves the given elementIds into the named workset.",
    {
      action: z.enum(["list", "create", "set_active", "assign"]).default("list").describe("Operation"),
      name: z.string().optional().describe("Workset name (create/set_active/assign)"),
      elementIds: z.array(z.number().int()).optional().describe("Elements to assign (assign mode)"),
    },
    async (args) => {
      const params = { ...args, name: args.name ?? "", elementIds: args.elementIds ?? [] };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("coord_manage_worksets", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `coord_manage_worksets failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
