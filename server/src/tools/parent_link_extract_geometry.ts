import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerParentLinkExtractGeometryTool(server: McpServer) {
  server.tool(
    "parent_link_extract_geometry",
    "PARENT TOOL — read-only first step of the linked-model → native pipeline. Inspect a loaded Revit/IFC link: returns element counts by category and (optionally) per-element geometry summaries (bbox min/max and volume in mm, with the link transform applied). Use this instead of hand-writing a send_code_to_revit recipe to read a link.",
    {
      linkName: z.string().optional().describe("Revit link name or id; omit for the first loaded link"),
      categories: z.array(z.string()).optional().describe("Restrict to these categories (BuiltInCategory names like OST_Walls, or display names). Omit for all model categories"),
      includeGeometry: z.boolean().optional().describe("Also extract per-element bbox/volume summaries (heavier). Default false"),
      maxElements: z.number().int().optional().describe("Cap on elements in the geometry list. Default 500"),
    },
    async (args) => {
      const params = {
        linkName: args.linkName ?? "",
        categories: args.categories ?? [],
        includeGeometry: args.includeGeometry ?? false,
        maxElements: args.maxElements ?? 500,
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("parent_link_extract_geometry", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `parent_link_extract_geometry failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
