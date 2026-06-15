import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerParentValidateDeviationTool(server: McpServer) {
  server.tool(
    "parent_validate_deviation",
    "PARENT TOOL — read-only QC for parent_reconstruct_native. For every CEMAI-tagged native rebuild, sample the source link's surfaces and measure the distance to the nearest native face. Returns per-item avg/worst deviation (mm), the count over tolerance, and overall avg/worst. Use after reconstructing to confirm the native model matches the linked source.",
    {
      linkName: z.string().optional().describe("Revit link name or id holding the source geometry; omit for the first loaded link"),
      appId: z.string().optional().describe("App-id tag used on the native rebuild. Default 'CEMAI'"),
      toleranceMm: z.number().optional().describe("Flag an item when its worst surface distance exceeds this (mm). Default 300"),
      maxElements: z.number().int().optional().describe("Cap on items validated. Default 1000"),
    },
    async (args) => {
      const params = {
        linkName: args.linkName ?? "",
        appId: args.appId ?? "CEMAI",
        toleranceMm: args.toleranceMm ?? 300,
        maxElements: args.maxElements ?? 1000,
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("parent_validate_deviation", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `parent_validate_deviation failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
