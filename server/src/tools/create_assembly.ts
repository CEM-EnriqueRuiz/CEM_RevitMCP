import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCreateAssemblyTool(server: McpServer) {
  server.tool(
    "create_assembly",
    "Create an assembly instance from member element ids (>=1). The assembly's naming category is taken from the first member. Optional assembly type name. Returns the assembly id, member count and name.",
    {
      elementIds: z.array(z.number().int()).min(1).describe("Member element ids"),
      name: z.string().optional().describe("Optional assembly type name"),
    },
    async (args) => {
      const params = { elementIds: args.elementIds, name: args.name ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("create_assembly", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `create_assembly failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
