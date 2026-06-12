import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetElementParametersTool(server: McpServer) {
  server.tool(
    "get_element_parameters",
    "Read instance (and optionally type) parameters from one or more elements: names, values, storage types, read-only flag, and BuiltInParameter where applicable. Read-back companion to set_element_parameters — use it to inspect before writing or to verify after.",
    {
      elementIds: z.array(z.number().int()).min(1).describe("Element ElementIds to read"),
      parameterNames: z
        .array(z.string())
        .optional()
        .describe("Optional whitelist of parameter names; omit to return all"),
      includeType: z.boolean().default(true).describe("Include type parameters too"),
      includeEmpty: z.boolean().default(false).describe("Include parameters that have no value"),
    },
    async (args) => {
      const params = {
        elementIds: args.elementIds,
        parameterNames: args.parameterNames ?? [],
        includeType: args.includeType,
        includeEmpty: args.includeEmpty,
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("get_element_parameters", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `get_element_parameters failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
