import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerGetWarningsTool(server: McpServer) {
  server.tool(
    "get_warnings",
    "Return the model's warnings (Document.GetWarnings) grouped by description with counts, severity, and a sample of affected element ids. The QA backbone — use it to review model health and decide what to fix.",
    {
      maxElementsPerWarning: z
        .number()
        .int()
        .positive()
        .default(20)
        .describe("Max sample element ids to return per warning group"),
    },
    async (args) => {
      try {
        const response = await withRevitConnection((c) => c.sendCommand("get_warnings", args));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `get_warnings failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
