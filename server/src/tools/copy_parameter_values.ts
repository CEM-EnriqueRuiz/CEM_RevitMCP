import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerCopyParameterValuesTool(server: McpServer) {
  server.tool(
    "copy_parameter_values",
    "Copy parameter values from a source element to many targets. Specify parameter names or omit to copy all writable matching values.",
    { sourceId: z.number().int().describe("Source element id"), targetIds: z.array(z.number().int()).min(1).describe("Target element ids"), parameterNames: z.array(z.string()).optional().describe("Names to copy; omit for all writable") },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("copy_parameter_values", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: "copy_parameter_values failed: " + (error instanceof Error ? error.message : String(error)) }] };
      }
    }
  );
}
