import { z } from "zod";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { withRevitConnection } from "../utils/ConnectionManager.js";

export function registerParentSolidToMemberParamsTool(server: McpServer) {
  server.tool(
    "parent_solid_to_member_params",
    "PARENT TOOL — read-only. Derive linear-member parameters from a linked element's geometry: best-fit centerline start/end, cross-section breadth×height, length and nearest level (all mm). Supply explicit linkElementIds or a category. Feed the result into struct_create_beam / struct_create_column / arch_create_wall to place fully-typed native elements.",
    {
      linkName: z.string().optional().describe("Revit link name or id; omit for the first loaded link"),
      linkElementIds: z.array(z.number().int()).optional().describe("Specific link element ids to measure"),
      category: z.string().optional().describe("Category to measure when no ids given (e.g. OST_StructuralFraming)"),
      roundMm: z.number().optional().describe("Round derived section/length to this step in mm (0 = none). Default 5"),
      maxElements: z.number().int().optional().describe("Cap on elements measured. Default 500"),
    },
    async (args) => {
      const params = {
        linkName: args.linkName ?? "",
        linkElementIds: args.linkElementIds ?? [],
        category: args.category ?? "",
        roundMm: args.roundMm ?? 5,
        maxElements: args.maxElements ?? 500,
      };
      try {
        const response = await withRevitConnection((c) => c.sendCommand("parent_solid_to_member_params", params));
        return { content: [{ type: "text", text: JSON.stringify(response, null, 2) }] };
      } catch (error) {
        return { content: [{ type: "text", text: `parent_solid_to_member_params failed: ${error instanceof Error ? error.message : String(error)}` }] };
      }
    }
  );
}
