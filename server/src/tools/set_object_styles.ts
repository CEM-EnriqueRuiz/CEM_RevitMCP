import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerSetObjectStylesTool(server: McpServer) {
  server.tool(
    "set_object_styles",
    "Set the object-style graphics for a category: projection/cut line weight (1-16), line color [r,g,b], and line pattern (by name/id). Category accepts a BuiltInCategory name or display name. Properties not applicable to the category are skipped with a warning.",
    {
      category: z.string().describe('Category, e.g. "OST_Walls" or "Walls"'),
      projectionLineWeight: z.number().int().min(1).max(16).optional().describe("Projection line weight"),
      cutLineWeight: z.number().int().min(1).max(16).optional().describe("Cut line weight (cuttable categories only)"),
      colorRgb: z.array(z.number().int().min(0).max(255)).length(3).optional().describe("Line color [r,g,b]"),
      linePatternName: z.string().optional().describe("Line pattern name or id to assign"),
    },
    async (args) => {
      const params = { ...args, linePatternName: args.linePatternName ?? "" };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("set_object_styles", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `set_object_styles failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
