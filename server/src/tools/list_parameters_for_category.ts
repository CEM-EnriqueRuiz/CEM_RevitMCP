import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerListParametersForCategoryTool(server: McpServer) {
  server.tool(
    "list_parameters_for_category",
    "List the parameters available on a category (sampled from a real element): names, storage types, read-only flag, and type-vs-instance. Use it to discover what is writable.",
    { category: z.string().describe("BuiltInCategory name, e.g. OST_Walls"), includeType: z.boolean().default(true) },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("list_parameters_for_category", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "list_parameters_for_category failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
