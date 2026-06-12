import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerFindElementsTool(server: McpServer) {
  server.tool(
    "find_elements",
    "Find element ids by category and optional parameter value (equals/contains), optionally active view only.",
    { categories: z.array(z.string()).min(1).describe("BuiltInCategory names"), parameterName: z.string().optional().describe("Parameter to filter on"), value: z.string().optional().describe("Value to match"), match: z.enum(["equals","contains"]).default("contains").describe("Match mode"), activeViewOnly: z.boolean().default(false), limit: z.number().int().positive().default(500) },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("find_elements", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "find_elements failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
